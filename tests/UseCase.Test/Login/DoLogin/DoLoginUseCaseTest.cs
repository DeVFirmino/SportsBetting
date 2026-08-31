using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using SportsBetting.Application.UseCases.User.Login.DoLogin;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Tests.Common.Cryptography;
using SportsBetting.Tests.Common.Entities;
using SportsBetting.Tests.Common.Repositories;
using SportsBetting.Tests.Common.Requests;
using SportsBetting.Tests.Common.Tokens;

namespace UseCase.Test.Login.DoLogin;

public class DoLoginUseCaseTest
{
    [Fact]
    public async Task ShouldReturnTokensWhenCredentialsAreValid()
    {
        (var user, var password) = UserBuilder.Build();

        var userCase = CreateUseCase(user);

        var result = await userCase.Execute(new LoginRequest
        {
            Email = user.Email,
            Password = password
        }, CancellationToken.None);

        result.Should().NotBeNull();
        result.Tokens.Should().NotBeNull();
        result.Name.Should().NotBeNullOrWhiteSpace().And.Be(user.Name);
        result.Tokens.AccessToken.Should().NotBeNullOrEmpty();

    }


    [Fact]
    public async Task ShouldThrowInvalidLoginWhenCredentialsAreInvalid()
    {
        var request = LoginRequestBuilder.Build();

        var useCase = CreateUseCase();

        Func<Task> action = async () => await useCase.Execute(request, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidLoginException>()
            .Where(e => e.Message.Equals(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID));
    }

    [Fact]
    public async Task ShouldRehashTheStoredPasswordWhenItUsesTheRetiredSha512Scheme()
    {
        // Load-bearing values: the retired scheme hashed "{password} {additionalKey}" as
        // uppercase-hex SHA-512, and this is the exact shape a pre-migration row carries.
        const string password = "current-password";
        const string additionalKey = "abc1234";

        var user = new SportsBetting.Domain.Entities.User
        {
            Id = 5,
            Name = "Legacy User",
            Email = "legacy@example.com",
            UserIdentifier = Guid.NewGuid(),
            Password = LegacySha512(password, additionalKey)
        };

        var useCase = CreateUseCase(user, additionalKey);

        var result = await useCase.Execute(new LoginRequest
        {
            Email = user.Email,
            Password = password
        }, CancellationToken.None);

        result.Tokens.AccessToken.Should().NotBeNullOrEmpty();
        user.Password.Should().NotBe(LegacySha512(password, additionalKey));
        PasswordHasherBuilder.Build().Verify(user, user.Password, password)
            .Should().Be(PasswordVerificationOutcome.Success);
    }

    [Fact]
    public async Task ShouldThrowInvalidLoginWhenTheLegacyPepperIsNotConfigured()
    {
        const string password = "current-password";

        var user = new SportsBetting.Domain.Entities.User
        {
            Id = 5,
            Name = "Legacy User",
            Email = "legacy@example.com",
            UserIdentifier = Guid.NewGuid(),
            Password = LegacySha512(password, "abc1234")
        };

        // Without the pepper the legacy hash cannot be verified, so the login fails closed.
        var useCase = CreateUseCase(user, legacyAdditionalKey: null);

        Func<Task> action = () => useCase.Execute(new LoginRequest
        {
            Email = user.Email,
            Password = password
        }, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidLoginException>();
    }

    [Fact]
    public async Task ShouldPayTheHashingCostWhenTheUserDoesNotExist()
    {
        var request = LoginRequestBuilder.Build();
        var hasherBuilder = new PasswordHasherMockBuilder();

        var useCase = CreateUseCase(passwordHasher: hasherBuilder.Build());

        Func<Task> action = () => useCase.Execute(request, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidLoginException>();

        // An unknown e-mail must still run one verification, so its response time cannot be told
        // apart from a wrong password's.
        hasherBuilder.VerifyWasInvokedOnce();
    }

    private static DoLoginUseCase CreateUseCase(
        SportsBetting.Domain.Entities.User? user = null,
        string? legacyAdditionalKey = null,
        IPasswordHasher? passwordHasher = null)
    {
        passwordHasher ??= PasswordHasherBuilder.Build(legacyAdditionalKey);
        var userReadOnlyRepositoryBuilder = new UserReadOnlyRepositoryBuilder();
        var userUpdateOnlyRepositoryBuilder = new UserUpdateOnlyRepositoryBuilder();
        var accessTokenGenerator = JwtTokenGeneratorBuilder.Build();
        if(user is not null)
        {
            userReadOnlyRepositoryBuilder.GetByEmailAsync(user);
            userUpdateOnlyRepositoryBuilder.GetById(user);
        }

        return new DoLoginUseCase(
            userReadOnlyRepositoryBuilder.Build(),
            userUpdateOnlyRepositoryBuilder.Build(),
            passwordHasher,
            accessTokenGenerator,
            UnitOfWorkBuilder.Build());
    }

    private static string LegacySha512(string password, string additionalKey) =>
        Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes($"{password} {additionalKey}")));
}

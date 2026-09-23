using FluentAssertions;
using SportsBetting.Application.UseCases.User.Login.DoLogin;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
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
        (SportsBetting.Domain.Entities.User? user, var password) = UserBuilder.Build();

        DoLoginUseCase userCase = CreateUseCase(user);

        AuthenticatedUserResponse result = await userCase.Execute(new LoginRequest
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
        LoginRequest request = LoginRequestBuilder.Build();

        DoLoginUseCase useCase = CreateUseCase();

        Func<Task> action = async () => await useCase.Execute(request, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidLoginException>()
            .Where(e => e.Message.Equals(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID));
    }

    [Fact]
    public async Task ShouldPayTheHashingCostWhenTheUserDoesNotExist()
    {
        LoginRequest request = LoginRequestBuilder.Build();
        var hasherBuilder = new PasswordHasherMockBuilder();

        DoLoginUseCase useCase = CreateUseCase(passwordHasher: hasherBuilder.Build());

        Func<Task> action = () => useCase.Execute(request, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidLoginException>();

        // An unknown e-mail must still run one verification, so its response time cannot be told
        // apart from a wrong password's.
        hasherBuilder.VerifyWasInvokedOnce();
    }

    private static DoLoginUseCase CreateUseCase(
        SportsBetting.Domain.Entities.User? user = null,
        IPasswordHasher? passwordHasher = null)
    {
        passwordHasher ??= PasswordHasherBuilder.Build();
        var userReadOnlyRepositoryBuilder = new UserReadOnlyRepositoryBuilder();
        IAccessTokenGenerator accessTokenGenerator = JwtTokenGeneratorBuilder.Build();
        if (user is not null)
        {
            userReadOnlyRepositoryBuilder.GetByEmailAsync(user);
        }

        return new DoLoginUseCase(
            userReadOnlyRepositoryBuilder.Build(),
            passwordHasher,
            accessTokenGenerator);
    }
}

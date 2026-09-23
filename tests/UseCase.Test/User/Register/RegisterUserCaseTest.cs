using AutoMapper;
using FluentAssertions;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.DataAccess.Repositories;
using SportsBetting.Tests.Common.Cryptography;
using SportsBetting.Tests.Common.Mapper;
using SportsBetting.Tests.Common.Repositories;
using SportsBetting.Tests.Common.Requests;
using SportsBetting.Tests.Common.Tokens;


namespace UseCase.Test.User.Register;

public class RegisterUserCaseTest
{
    [Fact]
    public async Task ShouldRegisterUserWhenRequestIsValid()
    {
        RegisterUserRequest request = RegisterUserRequestBuilder.Build();

        RegisterUserUseCase userCase = CreateUseCase();

        AuthenticatedUserResponse result = await userCase.Execute(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Tokens.Should().NotBeNull();
        result.Tokens.AccessToken.Should().NotBeNullOrEmpty();
        result.Name.Should().Be(request.Name);


    }

    [Fact]
    public async Task ShouldReturnErrorWhenEmailAlreadyExists()
    {
        RegisterUserRequest request = RegisterUserRequestBuilder.Build();

        RegisterUserUseCase userCase = CreateUseCase(request.Email);

        Func<Task> act = async () => await userCase.Execute(request, CancellationToken.None);

        (await act.Should().ThrowAsync<ErrorOnValidationException>())
            .Where(e => e.Errors.Count == 1 && e.Errors.Contains(ResourcesMessagesException.EMAIL_ALREADY_REGISTERED));


    }

    [Fact]
    public async Task ShouldReturnErrorWhenNameIsEmpty()
    {
        RegisterUserRequest request = RegisterUserRequestBuilder.Build();
        request.Name = string.Empty;

        RegisterUserUseCase userCase = CreateUseCase();

        Func<Task> act = async () => await userCase.Execute(request, CancellationToken.None);

        (await act.Should().ThrowAsync<ErrorOnValidationException>())
            .Where(e => e.Errors.Count == 1 && e.Errors.Contains(ResourcesMessagesException.NAME_EMPTY));


    }

    private RegisterUserUseCase CreateUseCase(string? email = null)
    {


        IMapper mapper = MapperBuilder.Build();
        IPasswordHasher passwordHasher = PasswordHasherBuilder.Build();
        IUserWriteOnlyRepository writeRepository = UserWriteOnlyRepositoryBuilder.Build();
        IUnitOfWork unitOfWork = UnitOfWorkBuilder.Build();
        var readRepositoryBuilder = new UserReadOnlyRepositoryBuilder();
        IAccessTokenGenerator accessTokenGenerator = JwtTokenGeneratorBuilder.Build();
        IWalletWriteOnlyRepository walletWriteOnlyRepository = WalletWriteOnlyRepositoryBuilder.Build();

        if (string.IsNullOrEmpty(email) == false)
        {
            readRepositoryBuilder.ExistsActiveUserWithEmailAsync(email);
        }

        return new RegisterUserUseCase(writeRepository, readRepositoryBuilder.Build(), mapper, passwordHasher, unitOfWork, accessTokenGenerator, walletWriteOnlyRepository);

    }
}

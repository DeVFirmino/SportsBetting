using FluentAssertions;
 using SportsBetting.Application.UseCases.User.Login.DoLogin;
using SportsBetting.Communication.Requests;
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
    public async Task Sucess()
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
    public async Task Error_Invalid_User()
    {
        var request = LoginRequestBuilder.Build();

        var useCase = CreateUseCase();
        
        Func<Task> action = async () => await useCase.Execute(request, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidLoginException>()
            .Where(e => e.Message.Equals(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID));
    }

    private static DoLoginUseCase CreateUseCase(SportsBetting.Domain.Entities.User? user = null)
    {
        var passwordEncrypter = PasswordEncrypterBuilder.Build();
        var userReadOnlyRepositoryBuilder = new UserReadOnlyRepositoryBuilder();
        var accessTokenGenerator = JwtTokenGeneratorBuilder.Build();
        
        if(user is not null)
            userReadOnlyRepositoryBuilder.GetByEmailAndPasswordAsync(user);
        
        return new DoLoginUseCase(userReadOnlyRepositoryBuilder.Build(), passwordEncrypter, accessTokenGenerator);
    }
    
    
}

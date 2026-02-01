using FluentAssertions;
using SportsBetting.Application.UseCases.User.Register;
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
    public async Task Sucess()
    {
        var request = RequestRegisterUserJsonBuilder.Build();
        
        var userCase = CreateUseCase();
 
       var result = await userCase.Execute(request);
       
       result.Should().NotBeNull();
       result.Tokens.Should().NotBeNull();
       result.Tokens.AccessToken.Should().NotBeNullOrEmpty();
       result.Name.Should().Be(request.Name);
        

    }
    
    [Fact]
    public async Task Error_Email_Already_Exist()
    {
        var request = RequestRegisterUserJsonBuilder.Build();
        
        var userCase = CreateUseCase(request.Email);
        
        Func<Task> act = async () => await userCase.Execute(request);

        (await act.Should().ThrowAsync<ErrorOnValidationException>())
            .Where(e => e.ErrorMessage.Count == 1 && e.ErrorMessage.Contains(ResourcesMessagesException.EMAIL_INVALID));


    }
    
    [Fact]
    public async Task Error_Name_Empty()
    {
        var request = RequestRegisterUserJsonBuilder.Build();
        request.Name = string.Empty;
        
        var userCase = CreateUseCase();
        
        Func<Task> act = async () => await userCase.Execute(request);

        (await act.Should().ThrowAsync<ErrorOnValidationException>())
            .Where(e => e.ErrorMessage.Count == 1 && e.ErrorMessage.Contains(ResourcesMessagesException.NAME_EMPTY));


    }

    private RegisterUserUseCase CreateUseCase(string? email = null)
    {
          

        var mapper = MapperBuilder.Build();
        var passwordEncrypter = PasswordEncrypterBuilder.Build();
        var writeRepository = UserWriteOnlyRepositoryBuilder.Build();
        var unitOfWork = UnitOfWorkBuilder.Build();
        var readRepositoryBuilder = new UserReadOnlyRepositoryBuilder(); 
        var accessTokenGenerator = JwtTokenGeneratorBuilder.Build();
        var walletWriteOnlyRepository = WalletWriteOnlyRepositoryBuilder.Build();

        if (string.IsNullOrEmpty(email) == false)
            readRepositoryBuilder.ExistActiveUserWithEmail(email);
            
        return new RegisterUserUseCase(writeRepository, readRepositoryBuilder.Build(), mapper, passwordEncrypter, unitOfWork, accessTokenGenerator, walletWriteOnlyRepository); 

    }
}

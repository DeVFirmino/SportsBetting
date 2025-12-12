using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Application.Services.Cryptography;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Exceptions.ExceptionBase;
  

namespace SportsBetting.Application.UseCases.User.Register;

public class RegisterUserUseCase
{
    private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
    private readonly IUserReadOnlyRepository _userReadOnlyRepository; 
    
    

    public async Task <ResponseRegisteredUserJson> Execute(RequestRegisterUserJson request)
    {
 
        var criptographyPassword = new PasswordEncrypter();
        
        var autoMapper = new AutoMapper.MapperConfiguration(options =>
        {
            options.AddProfile(new AutoMapping());
        }).CreateMapper();
        
        Validate(request);
        //Validate REQUEST  
        var user = autoMapper.Map<Domain.Entities.User>(request);
        //Criptography password
        user.Password = (criptographyPassword.Encrypt(request.Password));
 
        //Save on DB 
        
        await _userWriteOnlyRepository.Add(user);
        
        return new ResponseRegisteredUserJson()
        {
            Name = request.Name, //so vai funfar se tiver os 3 primeiros
        };
    }

    private void Validate(RequestRegisterUserJson request)
    {

        var validator = new RegisterUserValidator();

        var result = validator.Validate(request);

        if (result.IsValid == false)
        {
            var errorMessages = result.Errors.Select(e => e.ErrorMessage).ToList();
            
                throw new ErrorOnValidationException(errorMessages);

        }
    }
}
using AutoMapper;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Application.Services.Cryptography;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Exceptions.ExceptionBase;
  

namespace SportsBetting.Application.UseCases.User.Register;

public class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
    private readonly IUserReadOnlyRepository _userReadOnlyRepository;
    private readonly IMapper _mapper;
    private readonly PasswordEncrypter _passwordEncrypter;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserUseCase(IUserWriteOnlyRepository userWriteOnlyRepository,
        IUserReadOnlyRepository userReadOnlyRepository,
        IMapper mapper, PasswordEncrypter passwordEncrypter, IUnitOfWork unitOfWork)    
    {
        _userWriteOnlyRepository = userWriteOnlyRepository;
        _userReadOnlyRepository = userReadOnlyRepository;
        _mapper = mapper;
        _passwordEncrypter = passwordEncrypter;
        _unitOfWork = unitOfWork;
    }

    public async Task <ResponseRegisteredUserJson> Execute(RequestRegisterUserJson request)
    {
 
        Validate(request);
        // Map request to domain entity via AutoMapper
        var user = _mapper.Map<Domain.Entities.User>(request);
        
        //Criptography password
        user.Password = (_passwordEncrypter.Encrypt(request.Password));
        
        //Save on DB 
        await _userWriteOnlyRepository.Add(user);
        
        //unit of work 
        await _unitOfWork.Commit();
        
        return new ResponseRegisteredUserJson()
        {
            Name = request.Name,  
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
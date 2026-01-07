using AutoMapper;
using SportsBetting.Application.Services.AutoMapper;
 using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Exceptions;
  

namespace SportsBetting.Application.UseCases.User.Register;

public class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
    private readonly IUserReadOnlyRepository _userReadOnlyRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordEncrypter _passwordEncrypter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public RegisterUserUseCase(IUserWriteOnlyRepository userWriteOnlyRepository,
        IUserReadOnlyRepository userReadOnlyRepository,
        IMapper mapper, 
        IPasswordEncrypter passwordEncrypter,
        IUnitOfWork unitOfWork, 
        IAccessTokenGenerator accessTokenGenerator)    
    {
        _userWriteOnlyRepository = userWriteOnlyRepository;
        _userReadOnlyRepository = userReadOnlyRepository;
        _mapper = mapper;
        _passwordEncrypter = passwordEncrypter;
        _unitOfWork = unitOfWork;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task <ResponseRegisteredUserJson> Execute(RequestRegisterUserJson request)
    {

        await Validate(request);
        
        // Check if email already exists
        if (await _userReadOnlyRepository.ExistActiveUserWithEmail(request.Email))
        {
            throw new ErrorOnValidationException(new List<string>
            {
                ResourcesMessagesException.EMAIL_INVALID
            });
        }
        // Map request to domain entity via AutoMapper
        var user = _mapper.Map<Domain.Entities.User>(request);
        
        //Criptography password
        user.Password = (_passwordEncrypter.Encrypt(request.Password));
        user.UserIdentifier = Guid.NewGuid();
        
        //Save on DB 
        await _userWriteOnlyRepository.Add(user);
        
        //unit of work 
        await _unitOfWork.Commit();
        
        return new ResponseRegisteredUserJson()
        {
            Name = user.Name,
            Tokens = new ResponseTokenJson
            {
                AccessToken = _accessTokenGenerator.Generate(user.UserIdentifier)
            }        };
    }

    private async Task Validate(RequestRegisterUserJson request)
    {

        var validator = new RegisterUserValidator();

        var result = validator.Validate(request);
        
        var emailexist = await _userReadOnlyRepository.ExistActiveUserWithEmail(request.Email);
        if(emailexist)
            result.Errors.Add(new FluentValidation.Results.ValidationFailure
                (string.Empty, ResourcesMessagesException.EMAIL_INVALID));
        
        if (result.IsValid == false)
        {
            var errorMessages = result.Errors.Select(e => e.ErrorMessage).ToList();
            
                throw new ErrorOnValidationException(errorMessages);

        }
    }
}
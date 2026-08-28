using AutoMapper;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Exceptions;
  

namespace SportsBetting.Application.UseCases.User.Register;

public sealed class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
    private readonly IUserReadOnlyRepository _userReadOnlyRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordEncrypter _passwordEncrypter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IWalletWriteOnlyRepository _walletWriteOnlyRepository;

    public RegisterUserUseCase(IUserWriteOnlyRepository userWriteOnlyRepository,
        IUserReadOnlyRepository userReadOnlyRepository,
        IMapper mapper, 
        IPasswordEncrypter passwordEncrypter,
        IUnitOfWork unitOfWork, 
        IAccessTokenGenerator accessTokenGenerator,
        IWalletWriteOnlyRepository walletWriteOnlyRepository)     
    {
        _userWriteOnlyRepository = userWriteOnlyRepository;
        _userReadOnlyRepository = userReadOnlyRepository;
        _mapper = mapper;
        _passwordEncrypter = passwordEncrypter;
        _unitOfWork = unitOfWork;
        _accessTokenGenerator = accessTokenGenerator;
        _walletWriteOnlyRepository = walletWriteOnlyRepository;
    }

    public async Task<AuthenticatedUserResponse> Execute(RegisterUserRequest request, CancellationToken cancellationToken)
    {

        await Validate(request, cancellationToken);
        
         var user = _mapper.Map<Domain.Entities.User>(request);
        
         user.Password = (_passwordEncrypter.Encrypt(request.Password));
        user.UserIdentifier = Guid.NewGuid();
        
         await _userWriteOnlyRepository.AddAsync(user, cancellationToken);
        
         await _unitOfWork.CommitAsync(cancellationToken);
        
        var wallet = new Domain.Entities.Wallet()
        {
            UserId = user.Id,
            Balance = 0
        };
        
        await _walletWriteOnlyRepository.AddAsync(wallet, cancellationToken);

        
        await _unitOfWork.CommitAsync(cancellationToken);
        
         
        return new AuthenticatedUserResponse()
        {
            Name = user.Name,
            Tokens = new AccessTokenResponse
            {
                AccessToken = _accessTokenGenerator.Generate(user.UserIdentifier)
            }        };
    }

    private async Task Validate(RegisterUserRequest request, CancellationToken cancellationToken)
    {

        var validator = new RegisterUserValidator();

        var result = validator.Validate(request);
        
        var emailExist = await _userReadOnlyRepository.ExistsActiveUserWithEmailAsync(request.Email, cancellationToken);
                
        if(emailExist)
            result.Errors.Add(new FluentValidation.Results.ValidationFailure
                (string.Empty, ResourcesMessagesException.EMAIL_INVALID));
        
        if(!result.IsValid)
        {
            var errorMessages = result.Errors.Select(e => e.ErrorMessage).ToList();
            
                throw new ErrorOnValidationException(errorMessages);

        }
    }
}

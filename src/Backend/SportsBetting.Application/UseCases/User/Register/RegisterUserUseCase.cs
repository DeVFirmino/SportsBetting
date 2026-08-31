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
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IWalletWriteOnlyRepository _walletWriteOnlyRepository;

    public RegisterUserUseCase(IUserWriteOnlyRepository userWriteOnlyRepository,
        IUserReadOnlyRepository userReadOnlyRepository,
        IMapper mapper, 
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork, 
        IAccessTokenGenerator accessTokenGenerator,
        IWalletWriteOnlyRepository walletWriteOnlyRepository)     
    {
        _userWriteOnlyRepository = userWriteOnlyRepository;
        _userReadOnlyRepository = userReadOnlyRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _accessTokenGenerator = accessTokenGenerator;
        _walletWriteOnlyRepository = walletWriteOnlyRepository;
    }

    public async Task<AuthenticatedUserResponse> Execute(RegisterUserRequest request, CancellationToken cancellationToken)
    {

        await Validate(request, cancellationToken);
        
         var user = _mapper.Map<Domain.Entities.User>(request);
        
         user.Password = _passwordHasher.Hash(user, request.Password);
        user.UserIdentifier = Guid.NewGuid();
        
         await _userWriteOnlyRepository.AddAsync(user, cancellationToken);
        
        // The user has no identity value until the insert runs, so the wallet points at the user
        // through the navigation property. Entity Framework Core then orders the two inserts and
        // fills the foreign key itself, and registration stays a single atomic commit.
        var wallet = new Domain.Entities.Wallet()
        {
            User = user,
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

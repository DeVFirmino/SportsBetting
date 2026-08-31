 using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.User.Login.DoLogin;

public sealed class DoLoginUseCase : IDoLoginUseCase
{
    
    private readonly IUserReadOnlyRepository _repository;
    private readonly IUserUpdateOnlyRepository _userUpdateOnlyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    

    public DoLoginUseCase(IUserReadOnlyRepository repository, 
        IUserUpdateOnlyRepository userUpdateOnlyRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _userUpdateOnlyRepository = userUpdateOnlyRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<AuthenticatedUserResponse> Execute(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            throw new InvalidLoginException();
        }

        PasswordHashVerification verification = _passwordHasher.Verify(user, user.Password, request.Password);

        if (verification is PasswordHashVerification.Failed)
        {
            throw new InvalidLoginException();
        }

        if (verification is PasswordHashVerification.SuccessRehashNeeded)
        {
            user.Password = _passwordHasher.Hash(user, request.Password);
            _userUpdateOnlyRepository.Update(user);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        
        return new AuthenticatedUserResponse
        {
            Name = user.Name,
            Tokens = new AccessTokenResponse
            {
                AccessToken = _accessTokenGenerator.Generate(user.UserIdentifier)
            }
        };
    }
}

 using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.User.Login.DoLogin;

public sealed class DoLoginUseCase : IDoLoginUseCase
{
    
    private readonly IUserReadOnlyRepository _repository;
    private readonly IPasswordEncrypter _passwordEncrypter;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    

    public DoLoginUseCase(IUserReadOnlyRepository repository, 
        IPasswordEncrypter passwordEncrypter, 
        IAccessTokenGenerator accessTokenGenerator)
    {
        _repository = repository;
        _passwordEncrypter = passwordEncrypter;
        _accessTokenGenerator = accessTokenGenerator;
    }
    
    public async Task<AuthenticatedUserResponse> Execute(LoginRequest request, CancellationToken cancellationToken)
    {
        var encryptedPassword = _passwordEncrypter.Encrypt(request.Password);
        
        var user = await _repository.GetByEmailAndPasswordAsync(request.Email, encryptedPassword, cancellationToken);

        if (user is null)
        {
            throw new InvalidLoginException();
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

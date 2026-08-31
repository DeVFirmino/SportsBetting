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
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public DoLoginUseCase(
        IUserReadOnlyRepository repository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<AuthenticatedUserResponse> Execute(LoginRequest request, CancellationToken cancellationToken)
    {
        Domain.Entities.User? user = await _repository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
            throw new InvalidLoginException();

        if (_passwordHasher.Verify(user, user.Password, request.Password) is false)
            throw new InvalidLoginException();

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

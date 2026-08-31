using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
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

    public DoLoginUseCase(
        IUserReadOnlyRepository repository,
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
        Domain.Entities.User? user = await _repository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
            throw new InvalidLoginException();

        PasswordVerificationOutcome outcome = _passwordHasher.Verify(user, user.Password, request.Password);

        if (outcome is PasswordVerificationOutcome.Failed)
            throw new InvalidLoginException();

        // A hash from the retired SHA-512 scheme only proves itself on a successful login, which
        // is therefore the one chance to upgrade it in place.
        if (outcome is PasswordVerificationOutcome.SuccessRehashRequired)
            await RehashPassword(user.Id, request.Password, cancellationToken);

        return new AuthenticatedUserResponse
        {
            Name = user.Name,
            Tokens = new AccessTokenResponse
            {
                AccessToken = _accessTokenGenerator.Generate(user.UserIdentifier)
            }
        };
    }

    private async Task RehashPassword(long userId, string password, CancellationToken cancellationToken)
    {
        Domain.Entities.User user = await _userUpdateOnlyRepository.GetByIdAsync(userId, cancellationToken);

        user.Password = _passwordHasher.Hash(user, password);

        _userUpdateOnlyRepository.Update(user);

        await _unitOfWork.CommitAsync(cancellationToken);
    }
}

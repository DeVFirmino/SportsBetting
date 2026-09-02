namespace SportsBetting.Domain.Repositories.User;

public interface IUserReadOnlyRepository
{
    Task<bool> ExistsActiveUserWithEmailAsync(string email, CancellationToken cancellationToken);

    Task<Entities.User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<bool> ExistsActiveUserWithIdentifierAsync(
        Guid userIdentifier,
        CancellationToken cancellationToken);

}

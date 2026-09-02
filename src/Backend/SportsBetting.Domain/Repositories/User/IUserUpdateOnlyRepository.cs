namespace SportsBetting.Domain.Repositories.User;

public interface IUserUpdateOnlyRepository
{
    Task<Entities.User> GetByIdAsync(long id, CancellationToken cancellationToken);

    void Update(Entities.User user);
}

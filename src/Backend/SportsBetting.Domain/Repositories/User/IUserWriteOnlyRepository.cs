namespace SportsBetting.Domain.Repositories.User;

public interface IUserWriteOnlyRepository
{
    Task AddAsync(Entities.User user, CancellationToken cancellationToken);
}

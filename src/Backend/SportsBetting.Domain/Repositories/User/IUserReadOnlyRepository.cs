namespace SportsBetting.Domain.Repositories.User;

public interface IUserReadOnlyRepository
{
    public Task<bool> ExistActiveUserWithEmail(string email);
}
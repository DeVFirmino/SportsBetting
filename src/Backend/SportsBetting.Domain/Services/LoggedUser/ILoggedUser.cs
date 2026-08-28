using SportsBetting.Domain.Entities;

namespace SportsBetting.Domain.Services.LoggedUser;

public interface ILoggedUser
{
    Task<User> GetUserAsync(CancellationToken cancellationToken);
}

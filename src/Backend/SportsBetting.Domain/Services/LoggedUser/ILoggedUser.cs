using SportsBetting.Domain.Entities;

namespace SportsBetting.Domain.Services.LoggedUser;

public interface ILoggedUser
{
    public Task<User> User();
}
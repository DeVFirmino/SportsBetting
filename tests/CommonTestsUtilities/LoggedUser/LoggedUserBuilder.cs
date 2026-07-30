using Moq;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Services.LoggedUser;

namespace CommonTestsUtilities.LoggedUser;

public class LoggedUserBuilder
{
    private readonly Mock<ILoggedUser> _loggedUser = new();

    public LoggedUserBuilder User(User user)
    {
        _loggedUser.Setup(l => l.User()).ReturnsAsync(user);
        return this;
    }

    public ILoggedUser Build() => _loggedUser.Object;
}

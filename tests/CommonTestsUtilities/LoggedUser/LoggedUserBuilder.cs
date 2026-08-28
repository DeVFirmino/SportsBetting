using Moq;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Services.LoggedUser;

namespace SportsBetting.Tests.Common.LoggedUser;

public class LoggedUserBuilder
{
    private readonly Mock<ILoggedUser> _loggedUser = new();

    public LoggedUserBuilder User(User user)
    {
        _loggedUser.Setup(l => l.GetUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return this;
    }

    public ILoggedUser Build() => _loggedUser.Object;
}

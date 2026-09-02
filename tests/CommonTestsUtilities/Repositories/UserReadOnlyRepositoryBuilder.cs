using Moq;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.User;

namespace SportsBetting.Tests.Common.Repositories;

public class UserReadOnlyRepositoryBuilder
{
    private readonly Mock<IUserReadOnlyRepository> _repository;

    public UserReadOnlyRepositoryBuilder()
    {
        _repository = new Mock<IUserReadOnlyRepository>();
    }

    public void ExistsActiveUserWithEmailAsync(string email)
    {
        _repository.Setup(r => r.ExistsActiveUserWithEmailAsync(
            email,
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    public void GetByEmailAsync(User user)
    {
        _repository.Setup(r => r.GetByEmailAsync(
            user.Email,
            It.IsAny<CancellationToken>())).ReturnsAsync(user);
    }
    public IUserReadOnlyRepository Build()
    {
        return _repository.Object;
    }


}

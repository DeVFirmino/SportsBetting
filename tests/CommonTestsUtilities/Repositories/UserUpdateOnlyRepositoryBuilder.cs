using Moq;
using SportsBetting.Domain.Repositories.User;

namespace SportsBetting.Tests.Common.Repositories;

public sealed class UserUpdateOnlyRepositoryBuilder
{
    private readonly Mock<IUserUpdateOnlyRepository> _repository = new();

    public UserUpdateOnlyRepositoryBuilder GetById(SportsBetting.Domain.Entities.User user)
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        return this;
    }

    public IUserUpdateOnlyRepository Build() => _repository.Object;
}

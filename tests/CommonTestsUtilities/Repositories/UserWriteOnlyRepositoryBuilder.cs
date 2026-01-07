using Moq;
using SportsBetting.Domain.Repositories.User;

namespace SportsBetting.Tests.Common.Repositories;

public class UserWriteOnlyRepositoryBuilder
{
    public static IUserWriteOnlyRepository Build()
    {
        var mock = new Mock<IUserWriteOnlyRepository>();

        return mock.Object;

    }
}

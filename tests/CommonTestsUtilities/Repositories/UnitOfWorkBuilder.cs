using Moq;
using SportsBetting.Domain.Repositories;

namespace SportsBetting.Tests.Common.Repositories;

public class UnitOfWorkBuilder
{
    public static IUnitOfWork Build()
    {
        var mock = new Mock<IUnitOfWork>();

        return mock.Object;

    }


}

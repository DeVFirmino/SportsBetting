using Moq;
using SportsBetting.Domain.Repositories.WalletRepository;

namespace SportsBetting.Tests.Common.Repositories;

public class WalletWriteOnlyRepositoryBuilder
{
    public static IWalletWriteOnlyRepository Build()
    {
        var mock = new Mock<IWalletWriteOnlyRepository>();

        return mock.Object;
    }
}

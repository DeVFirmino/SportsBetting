using Moq;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.WalletTransactionRepository;

namespace SportsBetting.Tests.Common.Repositories;

public sealed class WalletTransactionReadOnlyRepositoryBuilder
{
    private readonly Mock<IWalletTransactionReadOnlyRepository> _repository = new();

    public WalletTransactionReadOnlyRepositoryBuilder AlreadyApplied(WalletTransaction transaction)
    {
        _repository
            .Setup(repository => repository.GetByClientRequestIdAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        return this;
    }

    public IWalletTransactionReadOnlyRepository Build() => _repository.Object;
}

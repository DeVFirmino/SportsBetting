using Moq;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.WalletTransactionRepository;

namespace SportsBetting.Tests.Common.Repositories;

/// <summary>
/// Captures the append-only ledger entries a use case writes, so a test can assert what was
/// recorded without reaching for Moq itself.
/// </summary>
public sealed class WalletTransactionWriteOnlyRepositoryBuilder
{
    private readonly Mock<IWalletTransactionWriteOnlyRepository> _repository = new();

    public List<WalletTransaction> Recorded { get; } = [];

    public WalletTransactionWriteOnlyRepositoryBuilder()
    {
        _repository
            .Setup(repository => repository.AddAsync(
                It.IsAny<WalletTransaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<WalletTransaction, CancellationToken>((transaction, _) => Recorded.Add(transaction))
            .Returns(Task.CompletedTask);
    }

    public IWalletTransactionWriteOnlyRepository Build() => _repository.Object;
}

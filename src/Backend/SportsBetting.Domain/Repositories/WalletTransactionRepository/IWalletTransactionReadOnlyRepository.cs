using SportsBetting.Domain.Entities;

namespace SportsBetting.Domain.Repositories.WalletTransactionRepository;

public interface IWalletTransactionReadOnlyRepository
{
    Task<WalletTransaction?> GetByClientRequestIdAsync(
        long walletId,
        string clientRequestId,
        CancellationToken cancellationToken);
}

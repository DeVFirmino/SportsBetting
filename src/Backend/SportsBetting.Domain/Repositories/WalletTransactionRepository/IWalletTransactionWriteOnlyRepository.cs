using SportsBetting.Domain.Entities;

namespace SportsBetting.Domain.Repositories.WalletTransactionRepository;

public interface IWalletTransactionWriteOnlyRepository
{
    Task AddAsync(WalletTransaction transaction, CancellationToken cancellationToken);
}

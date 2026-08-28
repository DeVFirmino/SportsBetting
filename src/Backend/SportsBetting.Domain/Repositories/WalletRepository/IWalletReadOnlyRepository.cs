namespace SportsBetting.Domain.Repositories.WalletRepository;

public interface IWalletReadOnlyRepository
{
    Task<Entities.Wallet?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);

    Task<bool> ExistsWalletForUserAsync(long userId, CancellationToken cancellationToken);
}

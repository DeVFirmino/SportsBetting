namespace SportsBetting.Domain.Repositories.WalletRepository;

public interface IWalletUpdateOnlyRepository
{
    Task<Entities.Wallet?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);
}

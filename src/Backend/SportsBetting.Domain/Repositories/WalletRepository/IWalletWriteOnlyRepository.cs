namespace SportsBetting.Domain.Repositories.WalletRepository;

public interface IWalletWriteOnlyRepository
{
    Task AddAsync(Entities.Wallet wallet, CancellationToken cancellationToken);
}

namespace SportsBetting.Domain.Repositories.WalletRepository;

public interface IWalletUpdateOnlyRepository
{
    Task<Entities.Wallet> GetByIdAsync(long id, CancellationToken cancellationToken);
    
    void Update(Entities.Wallet wallet);
}

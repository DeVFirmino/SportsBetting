namespace SportsBetting.Domain.Repositories.WalletRepository;

public interface IWalletWriteOnlyRepository
{
    public Task Add (Entities.Wallet wallet);
}
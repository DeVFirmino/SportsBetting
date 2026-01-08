namespace SportsBetting.Domain.Repositories.WalletRepository;

public interface IWalletUpdateOnlyRepository
{
    public Task<Entities.Wallet> GetById(long id); 
    
    public void Update(Entities.Wallet wallet);
}
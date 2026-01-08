namespace SportsBetting.Domain.Repositories.WalletRepository;

public interface IWalletReadOnlyRepository 
{
    public Task <Entities.Wallet> GetByUserId (long userId);
    
    public Task<bool> ExistWalletForUser(long userId);
    
}
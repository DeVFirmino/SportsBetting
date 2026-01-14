using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.WalletRepository;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public class WalletRepository : IWalletReadOnlyRepository, IWalletWriteOnlyRepository, IWalletUpdateOnlyRepository
{
    private readonly SportsBettingDbContext _context;
    
    public WalletRepository(SportsBettingDbContext context)
    {
        _context = context;
    }
    
    public async Task Add(Wallet wallet)
    {
        await _context.Wallets.AddAsync(wallet);
    }
    
    public async Task<Wallet?> GetByUserId(long userId)
    {
        return await _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Active);
    }
 
    public async Task<bool> ExistWalletForUser(long userId)
    {
        return await _context.Wallets
            .AnyAsync(w => w.UserId == userId && w.Active);
     }

    public async Task<Wallet> GetById(long id)
    {
        return await _context.Wallets
            .FirstAsync(w => w.Id == id);
    }

    public void Update(Wallet wallet)
    {
        _context.Wallets
            .Update(wallet);    
    }
}   


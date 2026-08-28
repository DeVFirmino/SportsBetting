using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.WalletRepository;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public sealed class WalletRepository : IWalletReadOnlyRepository, IWalletWriteOnlyRepository, IWalletUpdateOnlyRepository
{
    private readonly SportsBettingDbContext _context;
    
    public WalletRepository(SportsBettingDbContext context)
    {
        _context = context;
    }
    
    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken)
    {
        await _context.Wallets.AddAsync(wallet, cancellationToken);
    }
    
    public async Task<Wallet?> GetByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        return await _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Active, cancellationToken);
    }
 
    public async Task<bool> ExistsWalletForUserAsync(long userId, CancellationToken cancellationToken)
    {
        return await _context.Wallets
            .AnyAsync(w => w.UserId == userId && w.Active, cancellationToken);
     }

    public async Task<Wallet> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _context.Wallets
            .FirstAsync(w => w.Id == id, cancellationToken);
    }

    public void Update(Wallet wallet)
    {
        _context.Wallets
            .Update(wallet);    
    }
}   

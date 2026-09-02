using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.WalletRepository;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public sealed class WalletRepository :
    IWalletReadOnlyRepository,
    IWalletWriteOnlyRepository,
    IWalletUpdateOnlyRepository
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
            .FirstOrDefaultAsync(wallet => wallet.UserId == userId && wallet.Active, cancellationToken);
    }

    async Task<Wallet?> IWalletUpdateOnlyRepository.GetByUserIdAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(wallet => wallet.UserId == userId && wallet.Active, cancellationToken);
    }
}

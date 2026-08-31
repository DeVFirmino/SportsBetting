using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.WalletTransactionRepository;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public sealed class WalletTransactionRepository
    : IWalletTransactionWriteOnlyRepository, IWalletTransactionReadOnlyRepository
{
    private readonly SportsBettingDbContext _context;

    public WalletTransactionRepository(SportsBettingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WalletTransaction transaction, CancellationToken cancellationToken)
    {
        await _context.WalletTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<WalletTransaction?> GetByClientRequestIdAsync(
        long walletId,
        string clientRequestId,
        CancellationToken cancellationToken)
    {
        return await _context.WalletTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                transaction => transaction.WalletId == walletId
                    && transaction.ClientRequestId == clientRequestId,
                cancellationToken);
    }
}

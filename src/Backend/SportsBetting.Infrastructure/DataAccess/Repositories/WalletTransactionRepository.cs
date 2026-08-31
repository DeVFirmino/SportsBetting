using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.WalletTransactionRepository;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public sealed class WalletTransactionRepository : IWalletTransactionWriteOnlyRepository
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
}

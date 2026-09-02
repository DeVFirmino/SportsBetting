using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.BetRepository;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public sealed class BetRepository : IBetReadOnlyRepository, IBetWriteOnlyRepository
{
    private readonly SportsBettingDbContext _context;

    public BetRepository(SportsBettingDbContext context)
    {
        _context = context;
    }

    public async Task<Bet?> GetByIdAsync(
        long id,
        long userId,
        CancellationToken cancellationToken)
    {
        return await _context.Bets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                bet => bet.Id == id && bet.UserId == userId && bet.Active,
                cancellationToken);
    }

    public async Task<Bet?> GetByIdempotencyKeyAsync(
        long userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await _context.Bets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                bet => bet.UserId == userId
                    && bet.IdempotencyKey == idempotencyKey
                    && bet.Active,
                cancellationToken);
    }

    public async Task<(List<Bet> Items, int TotalCount)> GetPagedByUserIdAsync(
        long userId,
        int pageNumber,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        IQueryable<Bet> query = _context.Bets
            .AsNoTracking()
            .Where(bet => bet.UserId == userId && bet.Active);

        if (startDate.HasValue)
            query = query.Where(bet => bet.PlacedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(bet => bet.PlacedAt <= endDate.Value);

        int totalCount = await query.CountAsync(cancellationToken);
        List<Bet> items = await query
            .OrderByDescending(bet => bet.PlacedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Bet bet, CancellationToken cancellationToken)
    {
        await _context.Bets.AddAsync(bet, cancellationToken);
    }
}

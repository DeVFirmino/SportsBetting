using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories.BetRepository;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public sealed class BetRepository : IBetReadOnlyRepository, IBetWriteOnlyRepository, IBetUpdateOnlyRepository
{
    private readonly SportsBettingDbContext _context;
    
    public BetRepository(SportsBettingDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Bet>> GetByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        return await _context.Bets
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<Bet?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _context.Bets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.Active, cancellationToken);
    }

    public async Task<(List<Bet> Items, int TotalCount)> GetPagedByUserIdAsync(
        long userId,
        int pageNumber,
        int pageSize,
        BetStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.Bets
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Active);

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(b => b.PlacedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(b => b.PlacedAt <= endDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.PlacedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Update(Bet bet)
    {
        _context.Bets.Update(bet);
    }

    public async Task AddAsync(Bet bet, CancellationToken cancellationToken)
    {
        await _context.AddAsync(bet, cancellationToken);
    }

    Task<Bet> IBetUpdateOnlyRepository.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _context.Bets.FirstAsync(b => b.Id == id && b.Active, cancellationToken);
    }
}

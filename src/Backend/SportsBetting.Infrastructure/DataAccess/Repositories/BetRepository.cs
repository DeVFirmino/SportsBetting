using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories.BetRepository;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public class BetRepository : IBetReadOnlyRepository, IBetWriteOnlyRepository, IBetUpdateOnlyRepository
{
    private readonly SportsBettingDbContext _context;
    
    public BetRepository(SportsBettingDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Bet>> GetByUserId(long userId)
    {
        return await _context.Bets
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Active)
            .ToListAsync();
    }

    public async Task<Bet?> GetById(long id)
    {
        return await _context.Bets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.Active);
    }

    public async Task<(List<Bet> Items, int TotalCount)> GetPagedByUserId(
        long userId,
        int pageNumber,
        int pageSize,
        BetStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
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

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(b => b.PlacedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public void Update(Bet bet)
    {
        _context.Bets.Update(bet);
    }

    public async Task Add(Bet bet)
    {
        await _context.AddAsync(bet);
    }

    Task<Bet> IBetUpdateOnlyRepository.GetById(long id)
    {
        return _context.Bets.FirstAsync(b => b.Id == id && b.Active);
    }
}
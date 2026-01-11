using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
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
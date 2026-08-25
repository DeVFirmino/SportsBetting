using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Repositories;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Infrastructure.DataAccess;

public class UnitOfWork : IUnitOfWork
{
    private readonly SportsBettingDbContext _dbContext;

    public UnitOfWork(SportsBettingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Commit()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // The wallet's rowversion moved between the read and this write, so another bet
            // settled first. Translated here so callers deal with a domain exception instead
            // of an EF one — without it the wallet's concurrency token had no effect on the
            // response, and a lost update surfaced as a generic 500.
            throw new ConcurrencyException();
        }
    }
}

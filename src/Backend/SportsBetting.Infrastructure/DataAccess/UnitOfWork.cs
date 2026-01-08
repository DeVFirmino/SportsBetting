using SportsBetting.Domain.Repositories;
using SportsBetting.Infrastructure.DataAccess;

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
        await _dbContext.SaveChangesAsync();
    }
}
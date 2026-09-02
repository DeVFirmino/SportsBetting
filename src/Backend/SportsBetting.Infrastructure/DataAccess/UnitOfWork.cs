using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Repositories;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Infrastructure.DataAccess;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SportsBettingDbContext _dbContext;

    public UnitOfWork(SportsBettingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException();
        }
        catch (DbUpdateException exception) when (IsIdempotencyConflict(exception))
        {
            throw new IdempotencyConflictException();
        }
    }

    private static bool IsIdempotencyConflict(DbUpdateException exception)
    {
        return exception.InnerException is SqlException
        {
            Number: 2601 or 2627,
        } sqlException
            && sqlException.Message.Contains(
                SportsBettingDbContext.BetIdempotencyIndexName,
                StringComparison.Ordinal);
    }
}

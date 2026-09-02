using SportsBetting.Domain.Entities;
namespace SportsBetting.Domain.Repositories.BetRepository;

public interface IBetReadOnlyRepository
{
    Task<Bet?> GetByIdAsync(long id, long userId, CancellationToken cancellationToken);
    Task<Bet?> GetByIdempotencyKeyAsync(
        long userId,
        string idempotencyKey,
        CancellationToken cancellationToken);
    Task<(List<Bet> Items, int TotalCount)> GetPagedByUserIdAsync(
        long userId,
        int pageNumber,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);
}

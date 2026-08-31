using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Repositories.BetRepository;

public interface IBetReadOnlyRepository
{
    Task<List<Bet>> GetByUserIdAsync(long userId, CancellationToken cancellationToken);

    Task<Bet?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<Bet?> GetByClientRequestIdAsync(
        long userId,
        string clientRequestId,
        CancellationToken cancellationToken);

    Task<(List<Bet> Items, int TotalCount)> GetPagedByUserIdAsync(
        long userId,
        int pageNumber,
        int pageSize,
        BetStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);
}

using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Repositories.BetRepository;

public interface IBetReadOnlyRepository
{
    Task<List<Bet>> GetByUserId(long userId);

    Task<Bet?> GetById(long id);

    Task<(List<Bet> Items, int TotalCount)> GetPagedByUserId(
        long userId,
        int pageNumber,
        int pageSize,
        BetStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null);
}
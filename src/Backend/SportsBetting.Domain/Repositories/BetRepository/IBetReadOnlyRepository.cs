namespace SportsBetting.Domain.Repositories.BetRepository;

public interface IBetReadOnlyRepository
{
    Task<List<Entities.Bet>> GetByUserId(long userId);

    Task<Entities.Bet?> GetById(long id);

}
namespace SportsBetting.Domain.Repositories.BetRepository;

public interface IBetUpdateOnlyRepository
{
    Task<Entities.Bet> GetById(long id);
    
    void Update(Entities.Bet bet);
}
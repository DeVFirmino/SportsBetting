namespace SportsBetting.Domain.Repositories.BetRepository;

public interface IBetUpdateOnlyRepository
{
    Task<Entities.Bet> GetByIdAsync(long id, CancellationToken cancellationToken);
    
    void Update(Entities.Bet bet);
}

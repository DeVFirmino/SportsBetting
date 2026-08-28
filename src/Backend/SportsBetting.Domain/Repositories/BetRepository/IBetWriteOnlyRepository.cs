namespace SportsBetting.Domain.Repositories.BetRepository;

public interface IBetWriteOnlyRepository
{
    Task AddAsync(Entities.Bet bet, CancellationToken cancellationToken);
}

namespace SportsBetting.Domain.Repositories.BetRepository;

public interface IBetWriteOnlyRepository
{
    Task Add(Entities.Bet bet);
}
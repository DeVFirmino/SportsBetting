using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.Bet.GetBetsById;

public interface IGetBetByIdUseCase
{
    Task<BetResponse> Execute(long id, CancellationToken cancellationToken);
}

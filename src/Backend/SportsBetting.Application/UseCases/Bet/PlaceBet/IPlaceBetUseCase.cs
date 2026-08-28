using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.Bet.PlaceBet;

public interface IPlaceBetUseCase
{
    Task<BetResponse> Execute(PlaceBetRequest request, CancellationToken cancellationToken);
}

using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.Bet.PlaceBet;

public interface IPlaceBetUseCase
{
    public Task<ResponseBetsJson> Execute(RequestPlaceBetJson request);
}
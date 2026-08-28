using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.Bet.GetUserBets;

public interface IGetUserBetsUseCase
{
    Task<PagedResponse<BetResponse>> Execute(GetUserBetsRequest request, CancellationToken cancellationToken);
}

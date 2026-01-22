using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.Bet.GetUserBets;

public interface IGetUserBetsUseCase
{
    Task<ResponseUserBetsJson> Execute();

}
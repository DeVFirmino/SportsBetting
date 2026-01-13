namespace SportsBetting.Application.UseCases.Bet.GetUserBets;

public interface IGetUserBetsUseCase
{
    Task<List<Communication.Responses.ResponseBetsJson>> Execute();
}
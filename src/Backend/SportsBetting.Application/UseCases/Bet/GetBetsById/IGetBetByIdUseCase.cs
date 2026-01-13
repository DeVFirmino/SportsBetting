using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.Bet.GetBetsById;

public interface IGetBetByIdUseCase
{
    public Task<ResponseBetsJson> Execute(long id);
}
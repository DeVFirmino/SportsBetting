using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.User.GetBalance;

public interface IGetBalanceUseCase
{
    public Task<ResponseWalletJson> Execute();
}
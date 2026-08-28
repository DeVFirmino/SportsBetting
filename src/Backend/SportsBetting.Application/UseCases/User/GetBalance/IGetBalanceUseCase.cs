using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.User.GetBalance;

public interface IGetBalanceUseCase
{
    Task<WalletBalanceResponse> Execute(CancellationToken cancellationToken);
}

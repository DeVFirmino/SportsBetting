using SportsBetting.Communication.Requests;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;

public interface IDepositUseCase
{
    public Task Execute(RequestDepositJson request);
}
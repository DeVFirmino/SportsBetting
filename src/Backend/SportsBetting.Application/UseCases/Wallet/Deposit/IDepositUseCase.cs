using SportsBetting.Communication.Requests;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;

public interface IDepositUseCase
{
    Task Execute(DepositRequest request, CancellationToken cancellationToken);
}

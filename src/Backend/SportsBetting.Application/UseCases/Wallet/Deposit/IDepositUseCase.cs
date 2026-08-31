using SportsBetting.Communication.Requests;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;

public interface IDepositUseCase
{
    Task Execute(DepositRequest request, string? idempotencyKey, CancellationToken cancellationToken);
}

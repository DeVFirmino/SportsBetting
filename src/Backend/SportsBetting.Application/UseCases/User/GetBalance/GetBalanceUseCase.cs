using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.User.GetBalance;

public sealed class GetBalanceUseCase : IGetBalanceUseCase
{
    private readonly IWalletReadOnlyRepository _walletRepository;
    private readonly ILoggedUser _loggedUser;

    public GetBalanceUseCase(
        IWalletReadOnlyRepository walletRepository,
        ILoggedUser loggedUser)
    {
        _walletRepository = walletRepository;
        _loggedUser = loggedUser;
    }

    public async Task<WalletBalanceResponse> Execute(CancellationToken cancellationToken)
    {
        Domain.Entities.User user = await _loggedUser.GetUserAsync(cancellationToken);
        Domain.Entities.Wallet? wallet = await _walletRepository.GetByUserIdAsync(
            user.Id,
            cancellationToken);

        if (wallet is null)
            throw new ResourceNotFoundException(ResourcesMessagesException.WALLET_NOT_FOUND);

        return new WalletBalanceResponse { Balance = wallet.Balance };
    }
}

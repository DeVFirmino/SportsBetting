using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.User.GetBalance;

public sealed class GetBalanceUseCase : IGetBalanceUseCase
{
    
    private readonly IWalletReadOnlyRepository _walletRepository;
    private readonly ILoggedUser _loggedUser;
    
    public GetBalanceUseCase(IWalletReadOnlyRepository walletRepository,
        ILoggedUser loggedUser)
    {
        _walletRepository = walletRepository;
        _loggedUser = loggedUser;
    }
    
    
    public async Task<WalletBalanceResponse> Execute(CancellationToken cancellationToken)
    {
         var loggedUser = await _loggedUser.GetUserAsync(cancellationToken);
        
         var wallet = await _walletRepository.GetByUserIdAsync(loggedUser.Id, cancellationToken);

        // 3. Se não existir, erro
        return new WalletBalanceResponse
        {
            Balance = wallet?.Balance ?? 0
        };

 

     }
}

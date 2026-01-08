using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.User.GetBalance;

public class GetBalanceUseCase : IGetBalanceUseCase
{
    
    private readonly IWalletReadOnlyRepository _walletRepository;
    private readonly ILoggedUser _loggedUser;
    
    public GetBalanceUseCase(IWalletReadOnlyRepository walletRepository,
        ILoggedUser loggedUser)
    {
        _walletRepository = walletRepository;
        _loggedUser = loggedUser;
    }
    
    
    public async Task<ResponseWalletJson> Execute()
    {
         var loggedUser = await _loggedUser.User();
        
         var wallet = await _walletRepository.GetByUserId(loggedUser.Id);

        // 3. Se não existir, erro
        return new ResponseWalletJson
        {
            Balance = wallet?.Balance ?? 0
        };

 

     }
}
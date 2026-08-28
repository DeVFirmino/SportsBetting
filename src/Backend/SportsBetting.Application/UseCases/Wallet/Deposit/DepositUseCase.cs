using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;



public sealed class DepositUseCase : IDepositUseCase
{
    
    private readonly IWalletWriteOnlyRepository _walletWriteOnlyRepository;
    private readonly IWalletUpdateOnlyRepository _walletUpdateOnlyRepository;
    private readonly IWalletReadOnlyRepository _walletReadOnlyRepository;
    private readonly ILoggedUser _loggedUser; 
    private readonly IUnitOfWork _unitOfWork;
    
    
    public DepositUseCase(
        IWalletWriteOnlyRepository walletWriteOnlyRepository,
        ILoggedUser loggedUser, 
        IWalletUpdateOnlyRepository walletUpdateOnlyRepository, 
        IWalletReadOnlyRepository walletReadOnlyRepository,
        IUnitOfWork unitOfWork)
    {
        _walletWriteOnlyRepository = walletWriteOnlyRepository;
        _loggedUser = loggedUser;
        _walletUpdateOnlyRepository = walletUpdateOnlyRepository;
        _walletReadOnlyRepository = walletReadOnlyRepository;
        _unitOfWork = unitOfWork;
    }


    
    
    
    public async Task Execute(DepositRequest request, CancellationToken cancellationToken)
    {
        await Validate(request);
            
       var loggedUser = await _loggedUser.GetUserAsync(cancellationToken);
       
       var wallet = await _walletReadOnlyRepository.GetByUserIdAsync(loggedUser.Id, cancellationToken);

       if (wallet is null)
       {
           wallet = new Domain.Entities.Wallet
           {
               UserId = loggedUser.Id,
               Balance = request.Amount
           };
           
           await _walletWriteOnlyRepository.AddAsync(wallet, cancellationToken);
       }
       else
       {
           var walletToUpdate = await _walletUpdateOnlyRepository.GetByIdAsync(wallet.Id, cancellationToken);
           walletToUpdate.Balance += request.Amount;
           
           _walletUpdateOnlyRepository.Update(walletToUpdate);
       }
       
       await _unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task Validate(DepositRequest request)
    {
        var validator = new DepositValidator();
        
        var result = await validator.ValidateAsync(request);

        if (!result.IsValid)
        {
            var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();
            throw new ErrorOnValidationException(errorMessages);
        }
    }
}

using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;



public class DepositUseCase : IDepositUseCase
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


    
    
    
    public async Task Execute(RequestDepositJson request)
    {
        await Validate(request);
            
       var loggedUser = await _loggedUser.User();
       
       var wallet = await _walletReadOnlyRepository.GetByUserId(loggedUser.Id);

       if (wallet is null)
       {
           wallet = new Domain.Entities.Wallet
           {
               UserId = loggedUser.Id,
               Balance = request.Amount
           };
           
           await _walletWriteOnlyRepository.Add(wallet);
       }
       else
       {
           var walletToUpdate = await _walletUpdateOnlyRepository.GetById(wallet.Id);
           walletToUpdate.Balance += request.Amount;
           
           _walletUpdateOnlyRepository.Update(walletToUpdate);
       }
       
       await _unitOfWork.Commit();
    }

    public async Task Validate(RequestDepositJson request)
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
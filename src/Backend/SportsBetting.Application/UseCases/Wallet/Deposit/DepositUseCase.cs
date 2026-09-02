using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;

public sealed class DepositUseCase : IDepositUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IWalletUpdateOnlyRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DepositUseCase(
        ILoggedUser loggedUser,
        IWalletUpdateOnlyRepository walletRepository,
        IUnitOfWork unitOfWork)
    {
        _loggedUser = loggedUser;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(DepositRequest request, CancellationToken cancellationToken)
    {
        await Validate(request, cancellationToken);

        Domain.Entities.User user = await _loggedUser.GetUserAsync(cancellationToken);
        Domain.Entities.Wallet? wallet = await _walletRepository.GetByUserIdAsync(
            user.Id,
            cancellationToken);

        if (wallet is null)
            throw new ResourceNotFoundException(ResourcesMessagesException.WALLET_NOT_FOUND);

        wallet.Deposit(request.Amount);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static async Task Validate(DepositRequest request, CancellationToken cancellationToken)
    {
        DepositValidator validator = new();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(request, cancellationToken);

        if (result.IsValid is false)
            throw new ErrorOnValidationException(result.Errors.Select(error => error.ErrorMessage).ToList());
    }
}

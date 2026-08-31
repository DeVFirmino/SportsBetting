using Microsoft.EntityFrameworkCore;
using SportsBetting.Application.Shared;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Repositories.WalletTransactionRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;

public sealed class DepositUseCase : IDepositUseCase
{
    private readonly IWalletWriteOnlyRepository _walletWriteOnlyRepository;
    private readonly IWalletUpdateOnlyRepository _walletUpdateOnlyRepository;
    private readonly IWalletReadOnlyRepository _walletReadOnlyRepository;
    private readonly IWalletTransactionWriteOnlyRepository _walletTransactionWriteOnlyRepository;
    private readonly IWalletTransactionReadOnlyRepository _walletTransactionReadOnlyRepository;
    private readonly ILoggedUser _loggedUser;
    private readonly IUnitOfWork _unitOfWork;

    public DepositUseCase(
        IWalletWriteOnlyRepository walletWriteOnlyRepository,
        ILoggedUser loggedUser,
        IWalletUpdateOnlyRepository walletUpdateOnlyRepository,
        IWalletReadOnlyRepository walletReadOnlyRepository,
        IWalletTransactionWriteOnlyRepository walletTransactionWriteOnlyRepository,
        IWalletTransactionReadOnlyRepository walletTransactionReadOnlyRepository,
        IUnitOfWork unitOfWork)
    {
        _walletWriteOnlyRepository = walletWriteOnlyRepository;
        _loggedUser = loggedUser;
        _walletUpdateOnlyRepository = walletUpdateOnlyRepository;
        _walletReadOnlyRepository = walletReadOnlyRepository;
        _walletTransactionWriteOnlyRepository = walletTransactionWriteOnlyRepository;
        _walletTransactionReadOnlyRepository = walletTransactionReadOnlyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(
        DepositRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        await Validate(request);

        string? clientRequestId = IdempotencyKey.Normalize(idempotencyKey);

        Domain.Entities.User loggedUser = await _loggedUser.GetUserAsync(cancellationToken);

        Domain.Entities.Wallet? existing = await _walletReadOnlyRepository.GetByUserIdAsync(
            loggedUser.Id,
            cancellationToken);

        // Two deposits of the same amount are legitimately distinct, so the rowversion cannot tell
        // a retry from a second deposit. Only the key can, and replaying it is what stops a client
        // retry after a timeout from crediting the balance twice.
        if (existing is not null
            && clientRequestId is not null
            && await AlreadyApplied(existing.Id, clientRequestId, cancellationToken))
            return;

        Domain.Entities.Wallet wallet = await ResolveWallet(existing, loggedUser.Id, cancellationToken);

        Domain.Entities.WalletTransaction entry = wallet.Deposit(request.Amount, clientRequestId);

        await _walletTransactionWriteOnlyRepository.AddAsync(entry, cancellationToken);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The reader above is a fast path; concurrent retries can both pass it and the loser
            // collides with the ledger's filtered unique index. The winner already credited the
            // balance, so replaying it is a no-op rather than an error.
            if (clientRequestId is null || wallet.Id == 0)
                throw;

            if (await AlreadyApplied(wallet.Id, clientRequestId, cancellationToken) is false)
                throw;
        }
    }

    private async Task<Domain.Entities.Wallet> ResolveWallet(
        Domain.Entities.Wallet? existing,
        long userId,
        CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            Domain.Entities.Wallet created = new() { UserId = userId };

            await _walletWriteOnlyRepository.AddAsync(created, cancellationToken);

            return created;
        }

        // The read above is no-tracking, so the UPDATE has to be computed from a tracked instance.
        Domain.Entities.Wallet tracked = await _walletUpdateOnlyRepository.GetByIdAsync(
            existing.Id,
            cancellationToken);

        _walletUpdateOnlyRepository.Update(tracked);

        return tracked;
    }

    private async Task<bool> AlreadyApplied(
        long walletId,
        string clientRequestId,
        CancellationToken cancellationToken)
    {
        return await _walletTransactionReadOnlyRepository.GetByClientRequestIdAsync(
            walletId,
            clientRequestId,
            cancellationToken) is not null;
    }

    private static async Task Validate(DepositRequest request)
    {
        DepositValidator validator = new();

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(request);

        if (result.IsValid is false)
            throw new ErrorOnValidationException(result.Errors.Select(error => error.ErrorMessage).ToList());
    }
}

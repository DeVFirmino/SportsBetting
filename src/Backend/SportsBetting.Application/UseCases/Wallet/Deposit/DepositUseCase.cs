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
        if (existing is not null && clientRequestId is not null)
        {
            Domain.Entities.WalletTransaction? applied = await FindAppliedEntry(
                existing.Id,
                clientRequestId,
                cancellationToken);

            if (applied is not null)
            {
                EnsureReplayMatches(applied, request);
                return;
            }
        }

        Domain.Entities.Wallet wallet = await ResolveWallet(existing, loggedUser.Id, cancellationToken);

        Domain.Entities.WalletTransaction entry = wallet.Deposit(request.Amount, clientRequestId);

        await _walletTransactionWriteOnlyRepository.AddAsync(entry, cancellationToken);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (clientRequestId is not null
            && exception is DbUpdateException or ConcurrencyException)
        {
            // The reader above is a fast path; concurrent retries can both pass it. The loser
            // then either collides with the ledger's filtered unique index, or — when the winner
            // already credited the wallet — loses the rowversion check. Either way the winner
            // applied this key, so replaying it is a no-op rather than an error.
            Domain.Entities.WalletTransaction? applied = await FindAppliedEntryAfterConflict(
                loggedUser.Id,
                wallet,
                clientRequestId,
                cancellationToken);

            if (applied is null)
                throw;

            EnsureReplayMatches(applied, request);
        }
    }

    /// <summary>
    /// A replayed key must carry the same request it was stored under: silently ignoring a
    /// different amount — or a key already spent by a bet — would tell the client the new payload
    /// was applied.
    /// </summary>
    private static void EnsureReplayMatches(
        Domain.Entities.WalletTransaction applied,
        DepositRequest request)
    {
        bool matches = applied.Type is Domain.Enums.WalletTransactionType.Deposit
            && applied.Amount == request.Amount;

        if (matches is false)
            throw new ErrorOnValidationException([ResourcesMessagesException.IDEMPOTENCY_KEY_REUSED]);
    }

    private async Task<Domain.Entities.WalletTransaction?> FindAppliedEntryAfterConflict(
        long userId,
        Domain.Entities.Wallet wallet,
        string clientRequestId,
        CancellationToken cancellationToken)
    {
        // When the losing request was also creating the wallet, its tracked instance has no id;
        // the winner's wallet, found by user, is the one that owns the ledger entry.
        long walletId = wallet.Id;

        if (walletId == 0)
        {
            Domain.Entities.Wallet? winner = await _walletReadOnlyRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

            if (winner is null)
                return null;

            walletId = winner.Id;
        }

        return await FindAppliedEntry(walletId, clientRequestId, cancellationToken);
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

    private Task<Domain.Entities.WalletTransaction?> FindAppliedEntry(
        long walletId,
        string clientRequestId,
        CancellationToken cancellationToken)
    {
        return _walletTransactionReadOnlyRepository.GetByClientRequestIdAsync(
            walletId,
            clientRequestId,
            cancellationToken);
    }

    private static async Task Validate(DepositRequest request)
    {
        DepositValidator validator = new();

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(request);

        if (result.IsValid is false)
            throw new ErrorOnValidationException(result.Errors.Select(error => error.ErrorMessage).ToList());
    }
}

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Application.Shared;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Repositories.WalletTransactionRepository;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Bet.PlaceBet;

public sealed class PlaceBetUseCase : IPlaceBetUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IMapper _mapper;
    private readonly IBetReadOnlyRepository _betReadOnlyRepository;
    private readonly IBetWriteOnlyRepository _betWriteOnlyRepository;
    private readonly IWalletUpdateOnlyRepository _walletUpdateOnlyRepository;
    private readonly IWalletReadOnlyRepository _walletReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFootballApiService _footballApiService;
    private readonly IWalletTransactionWriteOnlyRepository _walletTransactionWriteOnlyRepository;
    private readonly IWalletTransactionReadOnlyRepository _walletTransactionReadOnlyRepository;

    public PlaceBetUseCase(
        ILoggedUser loggedUser,
        IMapper mapper,
        IBetReadOnlyRepository betReadOnlyRepository,
        IBetWriteOnlyRepository betWriteOnlyRepository,
        IWalletUpdateOnlyRepository walletUpdateOnlyRepository,
        IWalletReadOnlyRepository walletReadOnlyRepository,
        IUnitOfWork unitOfWork,
        IFootballApiService footballApiService,
        IWalletTransactionWriteOnlyRepository walletTransactionWriteOnlyRepository,
        IWalletTransactionReadOnlyRepository walletTransactionReadOnlyRepository)
    {
        _loggedUser = loggedUser;
        _mapper = mapper;
        _betReadOnlyRepository = betReadOnlyRepository;
        _betWriteOnlyRepository = betWriteOnlyRepository;
        _walletUpdateOnlyRepository = walletUpdateOnlyRepository;
        _walletReadOnlyRepository = walletReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _footballApiService = footballApiService;
        _walletTransactionWriteOnlyRepository = walletTransactionWriteOnlyRepository;
        _walletTransactionReadOnlyRepository = walletTransactionReadOnlyRepository;
    }

    public async Task<BetResponse> Execute(
        PlaceBetRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Validate(request);

        string? clientRequestId = IdempotencyKey.Normalize(idempotencyKey);

        Domain.Entities.User loggedUser = await _loggedUser.GetUserAsync(cancellationToken);

        if (clientRequestId is not null)
        {
            Domain.Entities.Bet? replay = await FindByClientRequestId(
                loggedUser.Id,
                clientRequestId,
                cancellationToken);

            if (replay is not null)
                return Replay(replay, request);
        }

        Domain.Entities.Wallet wallet = await ValidateWallet(loggedUser.Id, request.Amount, cancellationToken);

        FixtureData fixture = await GetFixture(request.FixtureId, cancellationToken);

        Domain.Entities.Bet bet = CreateBet(request, loggedUser.Id, clientRequestId, fixture);

        await DeductFromWallet(wallet.Id, request.Amount, bet, cancellationToken);

        await _betWriteOnlyRepository.AddAsync(bet, cancellationToken);

        try
        {
            // A lost concurrency check surfaces as ConcurrencyException: the request was valid and
            // the balance moved underneath it, so the API answers 409 and the client can retry.
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (clientRequestId is not null
            && exception is DbUpdateException or ConcurrencyException)
        {
            // The pre-read above is only a fast path. Two requests carrying the same key can both
            // pass it, because the fixture lookup between the read and the commit is an external
            // HTTP call. The loser then fails in one of two shapes: it collides with the filtered
            // unique index, or — when the winner's debit already moved the wallet — it loses the
            // rowversion check. In both, the winner holds the key, and replaying it is what makes
            // the key idempotent under concurrency.
            Domain.Entities.Bet? winner = await FindByClientRequestId(
                loggedUser.Id,
                clientRequestId,
                cancellationToken);

            if (winner is not null)
                return Replay(winner, request);

            // The same key on a ledger entry means it was already spent by a deposit — a client
            // mistake, not a server fault.
            if (await KeyUsedByWalletLedger(wallet.Id, clientRequestId, cancellationToken))
                throw new ErrorOnValidationException([ResourcesMessagesException.IDEMPOTENCY_KEY_REUSED]);

            // Nothing to replay means the commit failed for an unrelated reason. Rethrowing keeps
            // the original exception — and its classification and logging — instead of reporting
            // a bet that was never placed.
            throw;
        }

        return _mapper.Map<BetResponse>(bet);
    }

    /// <summary>
    /// A replayed key must carry the same request it was stored under. Returning the stored bet
    /// for a different amount, fixture or market would tell the client the new payload was
    /// accepted.
    /// </summary>
    private BetResponse Replay(Domain.Entities.Bet winner, PlaceBetRequest request)
    {
        bool matches = winner.Amount == request.Amount
            && winner.FixtureId == request.FixtureId
            && winner.BetType.ToString() == request.BetType;

        if (matches is false)
            throw new ErrorOnValidationException([ResourcesMessagesException.IDEMPOTENCY_KEY_REUSED]);

        return _mapper.Map<BetResponse>(winner);
    }

    private async Task<bool> KeyUsedByWalletLedger(
        long walletId,
        string clientRequestId,
        CancellationToken cancellationToken)
    {
        return await _walletTransactionReadOnlyRepository.GetByClientRequestIdAsync(
            walletId,
            clientRequestId,
            cancellationToken) is not null;
    }

    private Task<Domain.Entities.Bet?> FindByClientRequestId(
        long userId,
        string clientRequestId,
        CancellationToken cancellationToken)
    {
        return _betReadOnlyRepository.GetByClientRequestIdAsync(userId, clientRequestId, cancellationToken);
    }

    private static void Validate(PlaceBetRequest request)
    {
        PlaceBetValidator validator = new();

        FluentValidation.Results.ValidationResult result = validator.Validate(request);

        if (result.IsValid is false)
        {
            List<string> errorMessages = result.Errors
                .Select(error => error.ErrorMessage).ToList();

            throw new ErrorOnValidationException(errorMessages);
        }
    }

    private async Task<Domain.Entities.Wallet> ValidateWallet(
        long userId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        Domain.Entities.Wallet? wallet = await _walletReadOnlyRepository.GetByUserIdAsync(userId, cancellationToken);

        if (wallet is null)
            throw new ErrorOnValidationException([ResourcesMessagesException.WALLET_NOT_FOUND]);

        if (wallet.Balance < amount)
            throw new ErrorOnValidationException([ResourcesMessagesException.INSUFFICIENT_BALANCE]);

        return wallet;
    }

    private async Task<FixtureData> GetFixture(int fixtureId, CancellationToken cancellationToken)
    {
        List<FixtureData> fixtures = await _footballApiService.GetUpcomingFixturesAsync(cancellationToken);

        FixtureData? fixture = fixtures.FirstOrDefault(f => f.FixtureId == fixtureId);

        if (fixture is null)
            throw new ErrorOnValidationException([ResourcesMessagesException.FIXTURE_NOT_FOUND]);

        return fixture;
    }

    private static (BetType BetType, decimal Odds) BetTypeAndOdds(string betType, FixtureData fixture)
    {
        return betType switch
        {
            "HomeWin" => (BetType.HomeWin, fixture.HomeWinOdds ?? 1.0m),
            "Draw" => (BetType.Draw, fixture.DrawOdds ?? 1.0m),
            "AwayWin" => (BetType.AwayWin, fixture.AwayWinOdds ?? 1.0m),
            _ => throw new ErrorOnValidationException([ResourcesMessagesException.BET_TYPE_REQUIRED]),
        };
    }

    private static Domain.Entities.Bet CreateBet(
        PlaceBetRequest request,
        long userId,
        string? clientRequestId,
        FixtureData fixture)
    {
        (BetType betType, decimal odds) = BetTypeAndOdds(request.BetType!, fixture);

        return Domain.Entities.Bet.Place(
            userId,
            request.FixtureId,
            request.Amount,
            betType,
            odds,
            $"{fixture.HomeTeam} vs {fixture.AwayTeam}",
            clientRequestId,
            DateTime.UtcNow);
    }

    private async Task DeductFromWallet(
        long walletId,
        decimal amount,
        Domain.Entities.Bet bet,
        CancellationToken cancellationToken)
    {
        Domain.Entities.Wallet wallet = await _walletUpdateOnlyRepository.GetByIdAsync(walletId, cancellationToken);

        // ValidateWallet checked a no-tracking snapshot, and the fixture lookup between the two
        // reads is an external HTTP call, so the balance may have moved. This tracked instance is
        // the one the UPDATE is computed from; only a check here keeps the balance from going
        // negative, because the rowversion cannot flag a write based on a fresh read.
        if (wallet.Balance < amount)
            throw new ErrorOnValidationException([ResourcesMessagesException.INSUFFICIENT_BALANCE]);

        Domain.Entities.WalletTransaction transaction = wallet.Debit(amount, bet);

        await _walletTransactionWriteOnlyRepository.AddAsync(transaction, cancellationToken);
        _walletUpdateOnlyRepository.Update(wallet);
    }
}

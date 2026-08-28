using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.Bet.PlaceBet;

public sealed class PlaceBetUseCase : IPlaceBetUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IMapper _mapper;
    private readonly IBetWriteOnlyRepository _betWriteOnlyRepository;
    private readonly IWalletUpdateOnlyRepository _walletUpdateOnlyRepository;
    private readonly IWalletReadOnlyRepository _walletReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFootballApiService _footballApiService;

    public PlaceBetUseCase(
        ILoggedUser loggedUser,
        IMapper mapper,
        IBetWriteOnlyRepository betWriteOnlyRepository,
        IWalletUpdateOnlyRepository walletUpdateOnlyRepository,
        IWalletReadOnlyRepository walletReadOnlyRepository,
        IUnitOfWork unitOfWork,
        IFootballApiService footballApiService)
    {
        _loggedUser = loggedUser;
        _mapper = mapper;
        _betWriteOnlyRepository = betWriteOnlyRepository;
        _walletUpdateOnlyRepository = walletUpdateOnlyRepository;
        _walletReadOnlyRepository = walletReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _footballApiService = footballApiService;
    }

    public async Task<BetResponse> Execute(PlaceBetRequest request, CancellationToken cancellationToken)
    {
        Validate(request);

        var loggedUser = await _loggedUser.GetUserAsync(cancellationToken);

        var wallet = await ValidateWallet(loggedUser.Id, request.Amount, cancellationToken);

        var fixture = await GetFixture(request.FixtureId, cancellationToken);

        var bet = CreateBet(request, loggedUser.Id, fixture);

        await DeductFromWallet(wallet.Id, request.Amount, cancellationToken);

        await _betWriteOnlyRepository.AddAsync(bet, cancellationToken);

        // A lost concurrency check surfaces as ConcurrencyException: the request was valid and
        // the balance moved underneath it, so the API answers 409 and the client can retry.
        await _unitOfWork.CommitAsync(cancellationToken);

        return _mapper.Map<BetResponse>(bet);
    }

    private static void Validate(PlaceBetRequest request)
    {
        var validator = new PlaceBetValidator();

        var result = validator.Validate(request);

        if (!result.IsValid)
        {
            var errorMessages = result.Errors
                .Select(error => error.ErrorMessage).ToList();

            throw new ErrorOnValidationException(errorMessages);
        }
    }

    private async Task<Domain.Entities.Wallet> ValidateWallet(
        long userId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var wallet = await _walletReadOnlyRepository.GetByUserIdAsync(userId, cancellationToken);

        if (wallet is null)
            throw new ErrorOnValidationException([ResourcesMessagesException.WALLET_NOT_FOUND]);

        if (wallet.Balance < amount)
            throw new ErrorOnValidationException([ResourcesMessagesException.INSUFFICIENT_BALANCE]);

        return wallet;
    }

    private async Task<FixtureData> GetFixture(int fixtureId, CancellationToken cancellationToken)
    {
        var fixtures = await _footballApiService.GetUpcomingFixturesAsync(cancellationToken);

        var fixture = fixtures.FirstOrDefault(f => f.FixtureId == fixtureId);

        if (fixture is null)
            throw new ErrorOnValidationException([ResourcesMessagesException.FIXTURE_NOT_FOUND]);

        return fixture;
    }

    private void SetBetTypeAndOdds(Domain.Entities.Bet bet, string betType, FixtureData fixture)
    {
        if (betType == "HomeWin")
        {
            bet.BetType = BetType.HomeWin;
            bet.Odds = fixture.HomeWinOdds ?? 1.0m;
        }
        else if (betType == "Draw")
        {
            bet.BetType = BetType.Draw;
            bet.Odds = fixture.DrawOdds ?? 1.0m;
        }
        else if (betType == "AwayWin")
        {
            bet.BetType = BetType.AwayWin;
            bet.Odds = fixture.AwayWinOdds ?? 1.0m;
        }
        else
        {
            throw new ErrorOnValidationException([ResourcesMessagesException.BET_TYPE_REQUIRED]);
        }
    }

    private Domain.Entities.Bet CreateBet(PlaceBetRequest request, long userId, FixtureData fixture)
    {
        var bet = _mapper.Map<Domain.Entities.Bet>(request);

        bet.UserId = userId;
        bet.Status = BetStatus.Pending;
        bet.PlacedAt = DateTime.UtcNow;
        bet.EventName = $"{fixture.HomeTeam} vs {fixture.AwayTeam}";

        SetBetTypeAndOdds(bet, request.BetType!, fixture);

        bet.PotentialWinning = bet.Amount * bet.Odds;

        return bet;
    }

    private async Task DeductFromWallet(long walletId, decimal amount, CancellationToken cancellationToken)
    {
        var wallet = await _walletUpdateOnlyRepository.GetByIdAsync(walletId, cancellationToken);

        // ValidateWallet checked a no-tracking snapshot, and the fixture lookup between the two
        // reads is an external HTTP call, so the balance may have moved. This tracked instance is
        // the one the UPDATE is computed from; only a check here keeps the balance from going
        // negative, because the rowversion cannot flag a write based on a fresh read.
        if (wallet.Balance < amount)
            throw new ErrorOnValidationException([ResourcesMessagesException.INSUFFICIENT_BALANCE]);

        wallet.Balance -= amount;
        _walletUpdateOnlyRepository.Update(wallet);
    }

}

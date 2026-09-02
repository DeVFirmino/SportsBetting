using AutoMapper;
using SportsBetting.Application.Shared;
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
using ApiBettingMarket = SportsBetting.Communication.Enums.BettingMarket;

namespace SportsBetting.Application.UseCases.Bet.PlaceBet;

public sealed class PlaceBetUseCase : IPlaceBetUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IMapper _mapper;
    private readonly IBetReadOnlyRepository _betReadOnlyRepository;
    private readonly IBetWriteOnlyRepository _betWriteOnlyRepository;
    private readonly IWalletUpdateOnlyRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFootballApiService _footballApiService;

    public PlaceBetUseCase(
        ILoggedUser loggedUser,
        IMapper mapper,
        IBetReadOnlyRepository betReadOnlyRepository,
        IBetWriteOnlyRepository betWriteOnlyRepository,
        IWalletUpdateOnlyRepository walletRepository,
        IUnitOfWork unitOfWork,
        IFootballApiService footballApiService)
    {
        _loggedUser = loggedUser;
        _mapper = mapper;
        _betReadOnlyRepository = betReadOnlyRepository;
        _betWriteOnlyRepository = betWriteOnlyRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _footballApiService = footballApiService;
    }

    public async Task<BetResponse> Execute(
        PlaceBetRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        await Validate(request, cancellationToken);

        string key = IdempotencyKey.Normalize(idempotencyKey);
        BettingMarket market = ParseMarket(request.Market!.Value);
        Domain.Entities.User user = await _loggedUser.GetUserAsync(cancellationToken);

        Domain.Entities.Bet? replay = await FindByIdempotencyKey(user.Id, key, cancellationToken);
        if (replay is not null)
            return Replay(replay, request, market);

        FixtureData fixture = await GetFixture(request.FixtureId, cancellationToken);
        Domain.Entities.Wallet? wallet = await _walletRepository.GetByUserIdAsync(user.Id, cancellationToken);

        if (wallet is null)
            throw new ResourceNotFoundException(ResourcesMessagesException.WALLET_NOT_FOUND);

        if (wallet.Balance < request.Stake)
            throw new ErrorOnValidationException([ResourcesMessagesException.INSUFFICIENT_BALANCE]);

        decimal odds = GetOdds(market, fixture);
        Domain.Entities.Bet bet = Domain.Entities.Bet.Place(
            user.Id,
            request.FixtureId,
            request.Stake,
            market,
            odds,
            $"{fixture.HomeTeam} vs {fixture.AwayTeam}",
            key,
            DateTime.UtcNow);

        wallet.Debit(request.Stake);
        await _betWriteOnlyRepository.AddAsync(bet, cancellationToken);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is IdempotencyConflictException or ConcurrencyException)
        {
            Domain.Entities.Bet? winner = await FindByIdempotencyKey(user.Id, key, cancellationToken);

            if (winner is null)
                throw;

            return Replay(winner, request, market);
        }

        return _mapper.Map<BetResponse>(bet);
    }

    private BetResponse Replay(
        Domain.Entities.Bet bet,
        PlaceBetRequest request,
        BettingMarket market)
    {
        bool sameRequest = bet.FixtureId == request.FixtureId
            && bet.Stake == request.Stake
            && bet.Market == market;

        if (sameRequest is false)
            throw new IdempotencyConflictException();

        return _mapper.Map<BetResponse>(bet);
    }

    private Task<Domain.Entities.Bet?> FindByIdempotencyKey(
        long userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return _betReadOnlyRepository.GetByIdempotencyKeyAsync(
            userId,
            idempotencyKey,
            cancellationToken);
    }

    private async Task<FixtureData> GetFixture(int fixtureId, CancellationToken cancellationToken)
    {
        List<FixtureData> fixtures = await _footballApiService.GetFixturesAsync(cancellationToken);
        FixtureData? fixture = fixtures.FirstOrDefault(item => item.FixtureId == fixtureId);

        return fixture
            ?? throw new ErrorOnValidationException([ResourcesMessagesException.FIXTURE_NOT_FOUND]);
    }

    private static BettingMarket ParseMarket(ApiBettingMarket market)
    {
        return market switch
        {
            ApiBettingMarket.HomeWin => BettingMarket.HomeWin,
            ApiBettingMarket.Draw => BettingMarket.Draw,
            ApiBettingMarket.AwayWin => BettingMarket.AwayWin,
            _ => throw new ErrorOnValidationException([ResourcesMessagesException.BETTING_MARKET_REQUIRED]),
        };
    }

    private static decimal GetOdds(BettingMarket market, FixtureData fixture)
    {
        return market switch
        {
            BettingMarket.HomeWin => fixture.HomeWinOdds ?? 1m,
            BettingMarket.Draw => fixture.DrawOdds ?? 1m,
            BettingMarket.AwayWin => fixture.AwayWinOdds ?? 1m,
            _ => throw new ErrorOnValidationException([ResourcesMessagesException.BETTING_MARKET_REQUIRED]),
        };
    }

    private static async Task Validate(PlaceBetRequest request, CancellationToken cancellationToken)
    {
        PlaceBetValidator validator = new();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(request, cancellationToken);

        if (result.IsValid is false)
            throw new ErrorOnValidationException(result.Errors.Select(error => error.ErrorMessage).ToList());
    }
}

using FluentAssertions;
using SportsBetting.Infrastructure.Services.Odds;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
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
using SportsBetting.Tests.Common.Mapper;
using BetEntity = SportsBetting.Domain.Entities.Bet;
using UserEntity = SportsBetting.Domain.Entities.User;
using WalletEntity = SportsBetting.Domain.Entities.Wallet;
using ApiBettingMarket = SportsBetting.Communication.Enums.BettingMarket;

namespace UseCase.Test.Bet;

public sealed class PlaceBetUseCaseTests
{
    public static TheoryData<ApiBettingMarket, BettingMarket, decimal> SupportedMarkets => new()
    {
        { ApiBettingMarket.HomeWin, BettingMarket.HomeWin, 2.10m },
        { ApiBettingMarket.Draw, BettingMarket.Draw, 3.40m },
        { ApiBettingMarket.AwayWin, BettingMarket.AwayWin, 3.80m },
    };

    [Theory]
    [MemberData(nameof(SupportedMarkets))]
    public async Task ShouldPersistBetAndDebitWalletWhenRequestIsValid(
        ApiBettingMarket requestedMarket,
        BettingMarket expectedMarket,
        decimal expectedOdds)
    {
        TestContext context = CreateContext();
        PlaceBetRequest request = ValidRequest(requestedMarket);

        BetResponse response = await context.Execute(request, "key-1");

        response.Market.Should().Be(expectedMarket.ToString());
        response.Odds.Should().Be(expectedOdds);
        response.PotentialReturn.Should().Be(request.Stake * expectedOdds);
        context.Wallet!.Balance.Should().Be(80m);
        context.Bets.Persisted.Should().NotBeNull();
        context.UnitOfWork.CommitCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldReturnWalletNotFoundWhenWalletDoesNotExist()
    {
        TestContext context = CreateContext(walletExists: false);

        Func<Task> act = () => context.Execute(ValidRequest(), "key-1");

        (await act.Should().ThrowAsync<ResourceNotFoundException>())
            .Which.Errors.Should().ContainSingle(ResourcesMessagesException.WALLET_NOT_FOUND);
    }

    [Fact]
    public async Task ShouldReturnInsufficientBalanceWhenWalletCannotCoverStake()
    {
        TestContext context = CreateContext(balance: 10m);

        Func<Task> act = () => context.Execute(ValidRequest(), "key-1");

        (await act.Should().ThrowAsync<ErrorOnValidationException>())
            .Which.Errors.Should().ContainSingle(ResourcesMessagesException.INSUFFICIENT_BALANCE);
    }

    [Fact]
    public async Task ShouldReturnFixtureNotFoundWhenFixtureDoesNotExist()
    {
        TestContext context = CreateContext(fixtureExists: false);

        Func<Task> act = () => context.Execute(ValidRequest(), "key-1");

        (await act.Should().ThrowAsync<ErrorOnValidationException>())
            .Which.Errors.Should().ContainSingle(ResourcesMessagesException.FIXTURE_NOT_FOUND);
    }

    [Fact]
    public async Task ShouldReturnValidationErrorWhenRequestIsInvalid()
    {
        TestContext context = CreateContext();
        PlaceBetRequest request = ValidRequest();
        request.Stake = 0;

        Func<Task> act = () => context.Execute(request, "key-1");

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task ShouldReturnValidationErrorWhenIdempotencyKeyIsMissing()
    {
        TestContext context = CreateContext();

        Func<Task> act = () => context.Execute(ValidRequest(), null);

        (await act.Should().ThrowAsync<ErrorOnValidationException>())
            .Which.Errors.Should().ContainSingle(ResourcesMessagesException.IDEMPOTENCY_KEY_REQUIRED);
    }

    [Fact]
    public async Task ShouldReturnValidationErrorWhenIdempotencyKeyIsTooLong()
    {
        TestContext context = CreateContext();

        Func<Task> act = () => context.Execute(ValidRequest(), new string('k', 129));

        (await act.Should().ThrowAsync<ErrorOnValidationException>())
            .Which.Errors.Should().ContainSingle(ResourcesMessagesException.IDEMPOTENCY_KEY_TOO_LONG);
    }

    [Fact]
    public async Task ShouldReturnConflictWhenIdempotencyKeyWasStoredWithDifferentPayload()
    {
        BetEntity stored = StoredBet(stake: 10m);
        TestContext context = CreateContext(storedReplay: stored);

        Func<Task> act = () => context.Execute(ValidRequest(), stored.IdempotencyKey);

        await act.Should().ThrowAsync<IdempotencyConflictException>();
        context.Wallet!.Balance.Should().Be(100m);
        context.UnitOfWork.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task ShouldReturnStoredBetWhenIdempotencyKeyIsReplayedWithSamePayload()
    {
        BetEntity stored = StoredBet(stake: 20m);
        stored.Id = 42;
        TestContext context = CreateContext(storedReplay: stored);

        BetResponse response = await context.Execute(ValidRequest(), stored.IdempotencyKey);

        response.Id.Should().Be(42);
        context.Wallet!.Balance.Should().Be(100m);
        context.UnitOfWork.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task ShouldSurfaceConcurrencyConflictWhenCommitLosesRace()
    {
        TestContext context = CreateContext(commitException: new ConcurrencyException());

        Func<Task> act = () => context.Execute(ValidRequest(), "key-1");

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    private static TestContext CreateContext(
        decimal balance = 100m,
        bool walletExists = true,
        bool fixtureExists = true,
        FixtureData? fixture = null,
        BetEntity? storedReplay = null,
        Exception? commitException = null)
    {
        UserEntity user = new() { Id = 1, UserIdentifier = Guid.NewGuid() };
        WalletEntity? wallet = walletExists
            ? new WalletEntity { Id = 2, UserId = user.Id, Balance = balance }
            : null;
        BetRepositoryStub bets = new() { Existing = storedReplay };
        UnitOfWorkStub unitOfWork = new() { Exception = commitException };
        FootballApiStub footballApi = new()
        {
            Fixtures = fixtureExists ? [fixture ?? ValidFixture()] : [],
        };

        PlaceBetUseCase useCase = new(
            new LoggedUserStub(user),
            MapperBuilder.Build(),
            bets,
            bets,
            new WalletRepositoryStub(wallet),
            unitOfWork,
            footballApi,
            new FixedOddsService());

        return new TestContext(useCase, wallet, bets, unitOfWork);
    }

    private static PlaceBetRequest ValidRequest(ApiBettingMarket market = ApiBettingMarket.HomeWin) => new()
    {
        FixtureId = 10,
        Stake = 20m,
        Market = market,
    };

    private static FixtureData ValidFixture() => new()
    {
        FixtureId = 10,
        HomeTeam = "Home FC",
        AwayTeam = "Away FC",
    };

    private static BetEntity StoredBet(decimal stake)
    {
        return BetEntity.Place(
            1,
            10,
            stake,
            BettingMarket.HomeWin,
            2.5m,
            "Home FC vs Away FC",
            "key-1",
            DateTime.UtcNow);
    }

    private sealed class TestContext
    {
        private readonly PlaceBetUseCase _useCase;

        public TestContext(
            PlaceBetUseCase useCase,
            WalletEntity? wallet,
            BetRepositoryStub bets,
            UnitOfWorkStub unitOfWork)
        {
            _useCase = useCase;
            Wallet = wallet;
            Bets = bets;
            UnitOfWork = unitOfWork;
        }

        public WalletEntity? Wallet { get; }
        public BetRepositoryStub Bets { get; }
        public UnitOfWorkStub UnitOfWork { get; }

        public Task<BetResponse> Execute(PlaceBetRequest request, string? key)
        {
            return _useCase.Execute(request, key, CancellationToken.None);
        }
    }

    private sealed class LoggedUserStub : ILoggedUser
    {
        private readonly UserEntity _user;

        public LoggedUserStub(UserEntity user)
        {
            _user = user;
        }

        public Task<UserEntity> GetUserAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_user);
        }
    }

    private sealed class WalletRepositoryStub : IWalletUpdateOnlyRepository
    {
        private readonly WalletEntity? _wallet;

        public WalletRepositoryStub(WalletEntity? wallet)
        {
            _wallet = wallet;
        }

        public Task<WalletEntity?> GetByUserIdAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_wallet);
        }
    }

    private sealed class BetRepositoryStub : IBetReadOnlyRepository, IBetWriteOnlyRepository
    {
        public BetEntity? Existing { get; set; }
        public BetEntity? Persisted { get; private set; }

        public Task<BetEntity?> GetByIdAsync(
            long id,
            long userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<BetEntity?>(null);
        }

        public Task<BetEntity?> GetByIdempotencyKeyAsync(
            long userId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Existing?.IdempotencyKey == idempotencyKey ? Existing : null);
        }

        public Task<(List<BetEntity> Items, int TotalCount)> GetPagedByUserIdAsync(
            long userId,
            int pageNumber,
            int pageSize,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken cancellationToken)
        {
            return Task.FromResult((new List<BetEntity>(), 0));
        }

        public Task AddAsync(BetEntity bet, CancellationToken cancellationToken)
        {
            Persisted = bet;
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public Exception? Exception { get; set; }
        public int CommitCount { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            CommitCount++;

            if (Exception is not null)
                throw Exception;

            return Task.CompletedTask;
        }
    }

    private sealed class FootballApiStub : IFootballApiService
    {
        public List<FixtureData> Fixtures { get; set; } = [];

        public Task<List<FixtureData>> GetFixturesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Fixtures);
        }
    }
}

using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Tests.Common.Mapper;

namespace UseCase.Test.Bet;

public class PlaceBetUseCaseTests
{
    public static TheoryData<string, BetType, decimal> SupportedBetTypes => new()
    {
        { "HomeWin", BetType.HomeWin, 2.10m },
        { "Draw", BetType.Draw, 3.40m },
        { "AwayWin", BetType.AwayWin, 3.80m }
    };

    [Theory]
    [MemberData(nameof(SupportedBetTypes))]
    public async Task Execute_WithValidBet_PersistsBetAndDeductsBalance(
        string requestedType,
        BetType expectedType,
        decimal expectedOdds)
    {
        // Arrange
        var context = CreateContext();
        var request = ValidRequest(requestedType);

        // Act
        var result = await context.UseCase.Execute(request, CancellationToken.None);

        // Assert
        context.PersistedBet.Should().NotBeNull();
        context.PersistedBet!.UserId.Should().Be(context.User.Id);
        context.PersistedBet.FixtureId.Should().Be(request.FixtureId);
        context.PersistedBet.BetType.Should().Be(expectedType);
        context.PersistedBet.Odds.Should().Be(expectedOdds);
        context.PersistedBet.PotentialWinning.Should().Be(request.Amount * expectedOdds);
        context.PersistedBet.EventName.Should().Be("Home FC vs Away FC");
        context.PersistedBet.Status.Should().Be(BetStatus.Pending);
        context.Wallet.Balance.Should().Be(80m);
        result.PotentialWinning.Should().Be(request.Amount * expectedOdds);
    }

    [Fact]
    public async Task Execute_WithMissingOdds_UsesEvenOddsFallback()
    {
        // Arrange
        var fixture = Fixture();
        fixture.HomeWinOdds = null;
        var context = CreateContext(fixture: fixture);

        // Act
        var result = await context.UseCase.Execute(ValidRequest("HomeWin"), CancellationToken.None);

        // Assert
        context.PersistedBet!.Odds.Should().Be(1m);
        result.PotentialWinning.Should().Be(20m);
    }

    [Fact]
    public async Task Execute_WithoutWallet_ReturnsWalletNotFound()
    {
        // Arrange
        var context = CreateContext(walletExists: false);

        // Act
        Func<Task> act = () => context.UseCase.Execute(ValidRequest(), CancellationToken.None);

        // Assert
        await AssertSingleError(act, ResourcesMessagesException.WALLET_NOT_FOUND);
        context.PersistedBet.Should().BeNull();
    }

    [Fact]
    public async Task Execute_WithInsufficientBalance_ReturnsInsufficientBalance()
    {
        // Arrange
        var context = CreateContext(balance: 10m);

        // Act
        Func<Task> act = () => context.UseCase.Execute(ValidRequest(), CancellationToken.None);

        // Assert
        await AssertSingleError(act, ResourcesMessagesException.INSUFFICIENT_BALANCE);
        context.Wallet.Balance.Should().Be(10m);
    }

    [Fact]
    public async Task Execute_WithUnknownFixture_ReturnsFixtureNotFound()
    {
        // Arrange
        var context = CreateContext(fixtureExists: false);

        // Act
        Func<Task> act = () => context.UseCase.Execute(ValidRequest(), CancellationToken.None);

        // Assert
        await AssertSingleError(act, ResourcesMessagesException.FIXTURE_NOT_FOUND);
        context.Wallet.Balance.Should().Be(100m);
    }

    [Theory]
    [InlineData(0, "HomeWin")]
    [InlineData(20, "Invalid")]
    public async Task Execute_WithInvalidRequest_ReturnsValidationError(decimal amount, string betType)
    {
        // Arrange
        var context = CreateContext();
        var request = ValidRequest(betType);
        request.Amount = amount;

        // Act
        Func<Task> act = () => context.UseCase.Execute(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ErrorOnValidationException>();
        context.Wallet.Balance.Should().Be(100m);
    }

    [Fact]
    public async Task Execute_WhenCommitConflicts_SurfacesTheConcurrencyConflict()
    {
        // Arrange
        var context = CreateContext(commitException: new ConcurrencyException());

        // Act
        Func<Task> act = () => context.UseCase.Execute(ValidRequest(), CancellationToken.None);

        // Assert: a lost race is not a validation error — it reaches the API as a conflict,
        // which answers 409 and tells the client to retry.
        await act.Should().ThrowAsync<ConcurrencyException>()
            .WithMessage(ResourcesMessagesException.CONCURRENT_BET_DETECTED);
    }

    private static TestContext CreateContext(
        decimal balance = 100m,
        bool walletExists = true,
        bool fixtureExists = true,
        FixtureData? fixture = default,
        Exception? commitException = null)
    {
        var user = new SportsBetting.Domain.Entities.User { Id = 12 };
        var wallet = new SportsBetting.Domain.Entities.Wallet { Id = 31, UserId = user.Id, Balance = balance };

        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.GetUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var walletReadRepository = new Mock<IWalletReadOnlyRepository>();
        walletReadRepository.Setup(repository => repository.GetByUserIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(walletExists ? wallet : null!);

        var walletUpdateRepository = new Mock<IWalletUpdateOnlyRepository>();
        walletUpdateRepository.Setup(repository => repository.GetByIdAsync(
            wallet.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync(wallet);

        SportsBetting.Domain.Entities.Bet? persistedBet = null;
        var betRepository = new Mock<IBetWriteOnlyRepository>();
        betRepository.Setup(repository => repository.AddAsync(
                It.IsAny<SportsBetting.Domain.Entities.Bet>(),
                It.IsAny<CancellationToken>()))
            .Callback<SportsBetting.Domain.Entities.Bet, CancellationToken>((bet, _) => persistedBet = bet)
            .Returns(Task.CompletedTask);

        var footballApi = new Mock<IFootballApiService>();
        var fixtures = fixtureExists ? new List<FixtureData> { fixture ?? Fixture() } : [];
        footballApi.Setup(service => service.GetUpcomingFixturesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fixtures);

        var unitOfWork = new Mock<IUnitOfWork>();
        var commit = unitOfWork.Setup(work => work.CommitAsync(It.IsAny<CancellationToken>()));
        if (commitException is null)
        {
            commit.Returns(Task.CompletedTask);
        }
        else
        {
            commit.ThrowsAsync(commitException);
        }

        var useCase = new PlaceBetUseCase(
            loggedUser.Object,
            MapperBuilder.Build(),
            betRepository.Object,
            walletUpdateRepository.Object,
            walletReadRepository.Object,
            unitOfWork.Object,
            footballApi.Object);

        return new TestContext(useCase, user, wallet, () => persistedBet);
    }

    private static PlaceBetRequest ValidRequest(string betType = "HomeWin") => new()
    {
        FixtureId = 101,
        Amount = 20m,
        BetType = betType
    };

    private static FixtureData Fixture() => new()
    {
        FixtureId = 101,
        HomeTeam = "Home FC",
        AwayTeam = "Away FC",
        HomeWinOdds = 2.10m,
        DrawOdds = 3.40m,
        AwayWinOdds = 3.80m
    };

    private static async Task AssertSingleError(Func<Task> act, string expectedError)
    {
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().ContainSingle().Which.Should().Be(expectedError);
    }

    private sealed class TestContext
    {
        private readonly Func<SportsBetting.Domain.Entities.Bet?> _persistedBet;

        public TestContext(
            PlaceBetUseCase useCase,
            SportsBetting.Domain.Entities.User user,
            SportsBetting.Domain.Entities.Wallet wallet,
            Func<SportsBetting.Domain.Entities.Bet?> persistedBet)
        {
            UseCase = useCase;
            User = user;
            Wallet = wallet;
            _persistedBet = persistedBet;
        }

        public PlaceBetUseCase UseCase { get; }
        public SportsBetting.Domain.Entities.User User { get; }
        public SportsBetting.Domain.Entities.Wallet Wallet { get; }
        public SportsBetting.Domain.Entities.Bet? PersistedBet => _persistedBet();
    }
}

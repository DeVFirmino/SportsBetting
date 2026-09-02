using FluentAssertions;
using SportsBetting.Application.UseCases.Bet.GetBetsById;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Tests.Common.Mapper;
using BetEntity = SportsBetting.Domain.Entities.Bet;
using UserEntity = SportsBetting.Domain.Entities.User;

namespace UseCase.Test.Bet;

public sealed class GetBetByIdUseCaseTests
{
    [Fact]
    public async Task ShouldReturnMappedBetWhenBetExists()
    {
        BetEntity bet = BetEntity.Place(
            1,
            123,
            20m,
            BettingMarket.HomeWin,
            2.5m,
            "Home FC vs Away FC",
            "key-1",
            DateTime.UtcNow);
        bet.Id = 11;
        GetBetByIdUseCase useCase = CreateUseCase(bet);

        var result = await useCase.Execute(bet.Id, CancellationToken.None);

        result.Id.Should().Be(bet.Id);
        result.EventName.Should().Be(bet.EventName);
        result.PotentialReturn.Should().Be(50m);
    }

    [Fact]
    public async Task ShouldReturnBetNotFoundWhenBetIsMissing()
    {
        GetBetByIdUseCase useCase = CreateUseCase(null);

        Func<Task> act = () => useCase.Execute(999, CancellationToken.None);

        (await act.Should().ThrowAsync<ResourceNotFoundException>())
            .Which.Errors.Should().ContainSingle(ResourcesMessagesException.BET_NOT_FOUND);
    }

    [Fact]
    public async Task ShouldReturnBetNotFoundWhenBetBelongsToAnotherUser()
    {
        BetEntity bet = BetEntity.Place(
            99,
            123,
            20m,
            BettingMarket.HomeWin,
            2.5m,
            "Home FC vs Away FC",
            "key-1",
            DateTime.UtcNow);
        bet.Id = 11;
        GetBetByIdUseCase useCase = CreateUseCase(bet);

        Func<Task> act = () => useCase.Execute(bet.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    private static GetBetByIdUseCase CreateUseCase(BetEntity? bet)
    {
        return new GetBetByIdUseCase(
            new BetRepositoryStub(bet),
            MapperBuilder.Build(),
            new LoggedUserStub());
    }

    private sealed class BetRepositoryStub : IBetReadOnlyRepository
    {
        private readonly BetEntity? _bet;

        public BetRepositoryStub(BetEntity? bet)
        {
            _bet = bet;
        }

        public Task<BetEntity?> GetByIdAsync(
            long id,
            long userId,
            CancellationToken cancellationToken)
        {
            BetEntity? result = _bet is not null && _bet.Id == id && _bet.UserId == userId
                ? _bet
                : null;
            return Task.FromResult(result);
        }

        public Task<BetEntity?> GetByIdempotencyKeyAsync(
            long userId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<BetEntity?>(null);
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
    }

    private sealed class LoggedUserStub : ILoggedUser
    {
        public Task<UserEntity> GetUserAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new UserEntity { Id = 1 });
        }
    }
}

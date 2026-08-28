using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.Bet.GetBetsById;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Tests.Common.Mapper;
using Domain = SportsBetting.Domain;

namespace UseCase.Test.Bet;

public class GetBetByIdUseCaseTests
{
    [Fact]
    public async Task ShouldReturnMappedBetWhenBetExists()
    {
        // Arrange
        var bet = new Domain.Entities.Bet
        {
            Id = 11,
            UserId = 1,
            FixtureId = 123,
            Amount = 20m,
            Odds = 2.5m,
            PotentialWinning = 50m,
            EventName = "Home FC vs Away FC",
            BetType = BetType.HomeWin,
            Status = BetStatus.Pending
        };
        var useCase = CreateUseCase(bet);

        // Act
        var result = await useCase.Execute(bet.Id, CancellationToken.None);

        // Assert
        result.Id.Should().Be(bet.Id);
        result.EventName.Should().Be(bet.EventName);
        result.PotentialWinning.Should().Be(50m);
    }

    [Fact]
    public async Task ShouldReturnBetNotFoundWhenBetIsMissing()
    {
        // Arrange
        var useCase = CreateUseCase(null);

        // Act
        Func<Task> act = () => useCase.Execute(999, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().ContainSingle()
            .Which.Should().Be(ResourcesMessagesException.BET_NOT_FOUND);
    }

    [Fact]
    public async Task ShouldReturnBetNotFoundWhenBetBelongsToAnotherUser()
    {
        // Arrange
        var bet = new Domain.Entities.Bet { Id = 11, UserId = 99 };
        var useCase = CreateUseCase(bet);

        // Act
        Func<Task> act = () => useCase.Execute(bet.Id, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().ContainSingle()
            .Which.Should().Be(ResourcesMessagesException.BET_NOT_FOUND);
    }

    private static GetBetByIdUseCase CreateUseCase(Domain.Entities.Bet? bet)
    {
        var repository = new Mock<IBetReadOnlyRepository>();
        repository.Setup(item => item.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(bet);

        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.GetUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Domain.Entities.User { Id = 1 });

        return new GetBetByIdUseCase(repository.Object, MapperBuilder.Build(), loggedUser.Object);
    }
}

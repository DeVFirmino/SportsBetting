using FluentAssertions;
using Moq;
using SportsBetting.Infrastructure.Services.Odds;
using SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;
using SportsBetting.Domain.Services.ExternalApis;

namespace UseCase.Test.Fixture;

public class GetAvailableFixturesUseCaseTests
{
    [Fact]
    public async Task ShouldReturnFixtureWithServerOwnedOddsWhenFixturesAreAvailable()
    {
        // Arrange
        var date = new DateTime(2026, 8, 20, 19, 45, 0, DateTimeKind.Utc);
        var fixture = new FixtureData
        {
            FixtureId = 123,
            HomeTeam = "Home FC",
            AwayTeam = "Away FC",
            Date = date,
        };
        var service = new Mock<IFootballApiService>();
        service.Setup(api => api.GetFixturesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([fixture]);
        var useCase = new GetAvailableFixturesUseCase(service.Object, new FixedOddsService());

        // Act
        var result = await useCase.Execute(CancellationToken.None);

        // Assert
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            FixtureId = 123,
            HomeTeam = "Home FC",
            AwayTeam = "Away FC",
            Date = date,
            HomeWinOdds = 2.10m,
            DrawOdds = 3.40m,
            AwayWinOdds = 3.80m,
        });
    }

    [Fact]
    public async Task ShouldReturnEmptyCollectionWhenNoFixturesAreAvailable()
    {
        // Arrange
        var service = new Mock<IFootballApiService>();
        service.Setup(api => api.GetFixturesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var useCase = new GetAvailableFixturesUseCase(service.Object, new FixedOddsService());

        // Act
        var result = await useCase.Execute(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}

using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;
using SportsBetting.Domain.Services.ExternalApis;

namespace UseCase.Test.Fixture;

public class GetAvailableFixturesUseCaseTests
{
    [Fact]
    public async Task Execute_WithUpcomingFixtures_ReturnsAllMappedFields()
    {
        // Arrange
        var date = new DateTime(2026, 8, 20, 19, 45, 0, DateTimeKind.Utc);
        var fixture = new FixtureData
        {
            FixtureId = 123,
            HomeTeam = "Home FC",
            AwayTeam = "Away FC",
            Date = date,
            HomeWinOdds = 1.8m,
            DrawOdds = 3.2m,
            AwayWinOdds = 4.1m
        };
        var service = new Mock<IFootballApiService>();
        service.Setup(api => api.GetUpcomingFixtures()).ReturnsAsync([fixture]);
        var useCase = new GetAvailableFixturesUseCase(service.Object);

        // Act
        var result = await useCase.Execute();

        // Assert
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(fixture);
    }

    [Fact]
    public async Task Execute_WithoutUpcomingFixtures_ReturnsEmptyCollection()
    {
        // Arrange
        var service = new Mock<IFootballApiService>();
        service.Setup(api => api.GetUpcomingFixtures()).ReturnsAsync([]);
        var useCase = new GetAvailableFixturesUseCase(service.Object);

        // Act
        var result = await useCase.Execute();

        // Assert
        result.Should().BeEmpty();
    }
}

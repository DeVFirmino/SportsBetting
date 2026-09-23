using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Infrastructure.ExternalServices.Football;

namespace UseCase.Test.ExternalServices;

public class OfflineFootballApiServiceTests
{
    [Fact]
    public async Task ShouldReturnTheFixedCatalogueWhenFixturesAreRequested()
    {
        OfflineFootballApiService service = new(NullLogger<OfflineFootballApiService>.Instance);

        List<FixtureData> fixtures = await service.GetFixturesAsync(CancellationToken.None);

        fixtures.Select(fixture => fixture.FixtureId).Should().Equal(1001, 1002, 1003, 1004, 1005);
        fixtures[0].Should().BeEquivalentTo(new
        {
            FixtureId = 1001,
            HomeTeam = "Real Madrid",
            AwayTeam = "Barcelona",
        });
        fixtures.Should().AllSatisfy(fixture =>
        {
            fixture.HomeTeam.Should().NotBeNullOrWhiteSpace();
            fixture.AwayTeam.Should().NotBeNullOrWhiteSpace().And.NotBe(fixture.HomeTeam);
            fixture.Date.Kind.Should().Be(DateTimeKind.Utc);
        });
    }

    [Fact]
    public async Task ShouldReturnTheSameFixturesWhenCalledAgain()
    {
        OfflineFootballApiService service = new(NullLogger<OfflineFootballApiService>.Instance);
        List<FixtureData> first = await service.GetFixturesAsync(CancellationToken.None);

        List<FixtureData> second = await service.GetFixturesAsync(CancellationToken.None);

        // The README's request example names fixture 1001, so the ids must never move.
        second.Should().BeEquivalentTo(first, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ShouldNotShareFixtureInstancesWhenCalledAgain()
    {
        OfflineFootballApiService service = new(NullLogger<OfflineFootballApiService>.Instance);
        List<FixtureData> first = await service.GetFixturesAsync(CancellationToken.None);
        first[0].HomeTeam = "Changed by a caller";

        List<FixtureData> second = await service.GetFixturesAsync(CancellationToken.None);

        second[0].HomeTeam.Should().Be("Real Madrid");
    }
}

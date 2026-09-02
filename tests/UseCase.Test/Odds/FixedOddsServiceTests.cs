using FluentAssertions;
using SportsBetting.Domain.Services.Odds;
using SportsBetting.Infrastructure.Services.Odds;

namespace UseCase.Test.Odds;

public sealed class FixedOddsServiceTests
{
    [Fact]
    public void ShouldPriceAnyFixtureWithTheStudyOddsWhenAsked()
    {
        FixedOddsService service = new();

        FixtureOdds odds = service.GetOdds(fixtureId: 123);

        odds.HomeWin.Should().Be(2.10m);
        odds.Draw.Should().Be(3.40m);
        odds.AwayWin.Should().Be(3.80m);
    }
}

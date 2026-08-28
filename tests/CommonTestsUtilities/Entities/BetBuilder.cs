using Bogus;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Tests.Common.Entities;

public class BetBuilder
{
    public static List<Bet> Collection(int count = 5, long userId = 1)
    {
        var faker = new Faker<Bet>()
            .RuleFor(b => b.Id, f => f.Random.Long(1, 1000))
            .RuleFor(b => b.UserId, userId)
            .RuleFor(b => b.FixtureId, f => f.Random.Int(100, 999))
            .RuleFor(b => b.Amount, f => f.Random.Decimal(10, 100))
            .RuleFor(b => b.Odds, f => f.Random.Decimal(1.5m, 5.0m))
            .RuleFor(b => b.PotentialWinning, (f, b) => b.Amount * b.Odds)
            .RuleFor(b => b.BetType, f => BetType.HomeWin)
            .RuleFor(b => b.Status, f => BetStatus.Pending)
            .RuleFor(b => b.PlacedAt, f => f.Date.Recent())
            .RuleFor(b => b.Active, true);

        return faker.Generate(count);
    }
}

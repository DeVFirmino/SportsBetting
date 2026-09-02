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
            .RuleFor(b => b.Stake, f => f.Random.Decimal(10, 100))
            .RuleFor(b => b.Odds, f => f.Random.Decimal(1.5m, 5.0m))
            .RuleFor(b => b.PotentialReturn, (_, b) => b.Stake * b.Odds)
            .RuleFor(b => b.Market, _ => BettingMarket.HomeWin)
            .RuleFor(b => b.IdempotencyKey, f => f.Random.Guid().ToString())
            .RuleFor(b => b.PlacedAt, f => f.Date.Recent())
            .RuleFor(b => b.Active, true);

        return faker.Generate(count);
    }
}

using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Entities;

public sealed class Bet : EntityBase
{
    public long UserId { get; set; }
    public User User { get; set; } = default!;
    public int FixtureId { get; set; }
    public decimal Stake { get; set; }
    public decimal Odds { get; set; }
    public decimal PotentialReturn { get; set; }
    public string EventName { get; set; } = string.Empty;
    public BettingMarket Market { get; set; }
    public DateTime PlacedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;

    public static Bet Place(
        long userId,
        int fixtureId,
        decimal stake,
        BettingMarket market,
        decimal odds,
        string eventName,
        string idempotencyKey,
        DateTime placedAt)
    {
        if (stake <= 0)
            throw new ArgumentOutOfRangeException(nameof(stake));

        if (odds <= 0)
            throw new ArgumentOutOfRangeException(nameof(odds));

        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name is required.", nameof(eventName));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        return new Bet
        {
            UserId = userId,
            FixtureId = fixtureId,
            Stake = stake,
            Market = market,
            Odds = odds,
            PotentialReturn = stake * odds,
            EventName = eventName,
            PlacedAt = placedAt,
            IdempotencyKey = idempotencyKey,
        };
    }
}

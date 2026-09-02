using SportsBetting.Communication.Enums;

namespace SportsBetting.Communication.Responses;

public sealed class BetResponse
{
    public long Id { get; set; }

    public int FixtureId { get; set; }

    public decimal Stake { get; set; }

    public decimal Odds { get; set; }

    public decimal PotentialReturn { get; set; }
    public string EventName { get; set; } = string.Empty;
    public BettingMarket Market { get; set; }
    public DateTime PlacedAt { get; set; }
}

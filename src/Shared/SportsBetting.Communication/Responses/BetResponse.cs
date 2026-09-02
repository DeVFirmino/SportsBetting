namespace SportsBetting.Communication.Responses;

public sealed class BetResponse
{
    public long Id { get; set; }

    public int FixtureId { get; set; }

    public decimal Stake { get; set; }

    public decimal Odds { get; set; }

    public decimal PotentialReturn { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
}

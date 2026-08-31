namespace SportsBetting.Communication.Requests;

public sealed class PlaceBetRequest
{
    public int FixtureId { get; set; }

    public decimal Amount { get; set; }

    public string? BetType { get; set; }
}

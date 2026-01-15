namespace SportsBetting.Communication.Requests;

public class RequestPlaceBetJson
{
    public int FixtureId { get; set; }
    public decimal Amount { get; set; } 
    public string? BetType { get; set; }
}
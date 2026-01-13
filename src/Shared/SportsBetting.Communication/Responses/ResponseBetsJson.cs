namespace SportsBetting.Communication.Responses;

public class ResponseBetsJson
{
    public long Id { get; set; }
    
    public int FixtureId { get; set; }
    public decimal Amount { get; set; }
    public decimal Odds { get; set; }
    public decimal PotentialWinning { get; set; }
    public string EventName { get; set; }
    public string BetType { get; set; }
    public string Status { get; set; } 
    public DateTime PlacedAt { get; set; }
    
    
}
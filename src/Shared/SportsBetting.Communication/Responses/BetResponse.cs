namespace SportsBetting.Communication.Responses;

public sealed class BetResponse
{
    public long Id { get; set; }
    
    public int FixtureId { get; set; }
    
    public decimal Amount { get; set; }
    
    public decimal Odds { get; set; }
    
    public decimal PotentialWinning { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string BetType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    
    
}

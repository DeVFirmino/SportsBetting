namespace SportsBetting.Communication.Responses;

public class ResponseBetsJson
{
    public long Id { get; set; }
    
    public decimal Amount { get; set; }
    
    public decimal Odds { get; set; }
    public string EventName { get; set; }
    public string BetType { get; set; }
    public string Status { get; set; } 
    public DateTime PlacedAt { get; set; }
    public DateTime? SettledAt { get; set; }  
    
}
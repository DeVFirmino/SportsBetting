namespace SportsBetting.Communication.Requests;

public class RequestPlaceBetJson
{
    public decimal Amount { get; set; }
    
    public decimal Odds { get; set; }
    
    public string? EventName { get; set; }
    
    public string? BetType { get; set; }
}
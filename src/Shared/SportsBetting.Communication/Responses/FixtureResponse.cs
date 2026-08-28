namespace SportsBetting.Communication.Responses;

public sealed class FixtureResponse
{
    public int FixtureId { get; set; }
    
    public string HomeTeam { get; set; } = string.Empty;
    
    public string AwayTeam { get; set; } = string.Empty;
    
    public DateTime Date { get; set; }
    
    public decimal? HomeWinOdds { get; set; }
    
    public decimal? DrawOdds { get; set; }
    
    public decimal? AwayWinOdds { get; set; }
}

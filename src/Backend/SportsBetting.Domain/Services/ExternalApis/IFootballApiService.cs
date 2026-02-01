namespace SportsBetting.Domain.Services.ExternalApis;

public interface IFootballApiService
{
    Task<List<FixtureData>> GetUpcomingFixtures();
}

public class FixtureData
{
    public int FixtureId { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal? HomeWinOdds { get; set; }   
    public decimal? DrawOdds { get; set; }      
    public decimal? AwayWinOdds { get; set; }   
}

public class OddsData
{
    public decimal HomeWin { get; set; }
    public decimal Draw { get; set; }
    public decimal AwayWin { get; set; }
}


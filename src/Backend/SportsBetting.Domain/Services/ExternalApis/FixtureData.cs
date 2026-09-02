namespace SportsBetting.Domain.Services.ExternalApis;

public sealed class FixtureData
{
    public int FixtureId { get; set; }

    public string HomeTeam { get; set; } = string.Empty;

    public string AwayTeam { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}

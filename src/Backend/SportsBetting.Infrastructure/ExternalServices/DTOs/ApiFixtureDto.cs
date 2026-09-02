namespace SportsBetting.Infrastructure.ExternalServices.DTOs;

using System.Text.Json.Serialization;

public class ApiFixtureDto
{
    [JsonPropertyName("fixture")]
    public FixtureInfo Fixture { get; set; } = new();

    [JsonPropertyName("teams")]
    public TeamsInfo Teams { get; set; } = new();
}

public class FixtureInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}

public class TeamsInfo
{
    [JsonPropertyName("home")]
    public TeamInfo Home { get; set; } = new();

    [JsonPropertyName("away")]
    public TeamInfo Away { get; set; } = new();
}

public class TeamInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
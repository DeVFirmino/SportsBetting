using System.ComponentModel.DataAnnotations;

namespace SportsBetting.Infrastructure.Options;

public sealed class FootballApiOptions
{
    public const string SectionName = "Settings:FootballApi";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CacheSeconds { get; set; } = 30;

    // API-Football answers a fixed league season. The study reads one, so the fixture list is
    // stable for demos and never depends on the current date.
    [Range(2000, 2100)]
    public int Season { get; set; } = 2024;

    [Range(1, int.MaxValue)]
    public int LeagueId { get; set; } = 140;

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;
}

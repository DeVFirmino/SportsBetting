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
}

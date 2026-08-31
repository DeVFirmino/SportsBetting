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

    /// <summary>
    /// Base backoff between retries. Exposed because the right pause depends on the upstream plan
    /// this deployment is on, and a slow environment should not have to be rebuilt to widen it.
    /// </summary>
    [Range(1, 60_000)]
    public int RetryDelayMilliseconds { get; set; } = 500;
}

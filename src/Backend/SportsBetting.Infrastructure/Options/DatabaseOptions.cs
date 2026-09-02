using System.ComponentModel.DataAnnotations;

namespace SportsBetting.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string DefaultConnection { get; set; } = string.Empty;
}

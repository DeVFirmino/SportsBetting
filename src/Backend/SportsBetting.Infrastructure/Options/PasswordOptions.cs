namespace SportsBetting.Infrastructure.Options;

public sealed class PasswordOptions
{
    public const string SectionName = "Settings:Password";

    public string AdditionalKey { get; set; } = string.Empty;
}

namespace SportsBetting.Infrastructure.Options;

/// <summary>
/// The pepper of the retired SHA-512 password scheme. It is only needed to verify hashes stored
/// before the switch to the ASP.NET Core Identity hasher, so it is optional: a deployment with no
/// legacy users simply leaves it unset and legacy-format hashes never verify.
/// </summary>
public sealed class LegacyPasswordOptions
{
    public const string SectionName = "Settings:Password";

    public string AdditionalKey { get; set; } = string.Empty;
}

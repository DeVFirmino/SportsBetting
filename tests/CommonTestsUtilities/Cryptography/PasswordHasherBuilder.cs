using Microsoft.Extensions.Options;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Infrastructure.Options;
using SportsBetting.Infrastructure.Security.Cryptography;

namespace SportsBetting.Tests.Common.Cryptography;

public static class PasswordHasherBuilder
{
    /// <summary>
    /// The real hasher. Pass <paramref name="legacyAdditionalKey"/> only when a test exercises
    /// verification of hashes from the retired SHA-512 scheme.
    /// </summary>
    public static IPasswordHasher Build(string? legacyAdditionalKey = null) =>
        new IdentityPasswordHasher(Options.Create(new LegacyPasswordOptions
        {
            AdditionalKey = legacyAdditionalKey ?? string.Empty
        }));
}

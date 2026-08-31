using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Infrastructure.Options;

namespace SportsBetting.Infrastructure.Security.Cryptography;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private const int LegacyHashLength = 128;

    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly LegacyPasswordOptions _legacyOptions;

    public IdentityPasswordHasher(IOptions<LegacyPasswordOptions> legacyOptions)
    {
        _legacyOptions = legacyOptions.Value;
    }

    public string Hash(User user, string password) => _passwordHasher.HashPassword(user, password);

    public PasswordVerificationOutcome Verify(User user, string hashedPassword, string providedPassword)
    {
        // Hashes stored before the Identity hasher are 128 hex characters of SHA-512; the
        // Identity format is Base64 and never matches that shape. Verifying them here — instead
        // of rejecting them — is what keeps accounts created under the old scheme able to log in.
        if (IsLegacySha512(hashedPassword))
            return VerifyLegacy(hashedPassword, providedPassword);

        try
        {
            return _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword)
                is PasswordVerificationResult.Failed
                    ? PasswordVerificationOutcome.Failed
                    : PasswordVerificationOutcome.Success;
        }
        catch (FormatException)
        {
            // A stored value that is not in the hasher's format is corrupt data, not a wrong
            // password. Answering "no" keeps that out of the login path as a 401 instead of a 500.
            return PasswordVerificationOutcome.Failed;
        }
        catch (ArgumentException)
        {
            return PasswordVerificationOutcome.Failed;
        }
    }

    private static bool IsLegacySha512(string hashedPassword) =>
        hashedPassword.Length == LegacyHashLength
        && hashedPassword.All(character => character is (>= '0' and <= '9') or (>= 'A' and <= 'F'));

    private PasswordVerificationOutcome VerifyLegacy(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(_legacyOptions.AdditionalKey))
            return PasswordVerificationOutcome.Failed;

        // The retired scheme hashed "{password} {additionalKey}" — space included.
        byte[] computed = SHA512.HashData(
            Encoding.UTF8.GetBytes($"{providedPassword} {_legacyOptions.AdditionalKey}"));

        byte[] stored = Convert.FromHexString(hashedPassword);

        return CryptographicOperations.FixedTimeEquals(computed, stored)
            ? PasswordVerificationOutcome.SuccessRehashRequired
            : PasswordVerificationOutcome.Failed;
    }
}

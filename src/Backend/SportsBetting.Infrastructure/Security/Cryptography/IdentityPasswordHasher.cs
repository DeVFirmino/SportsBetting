using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Security.Cryptography;
using PasswordSettings = SportsBetting.Infrastructure.Options.PasswordOptions;

namespace SportsBetting.Infrastructure.Security.Cryptography;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly Sha512Encrypter _legacyHasher;

    public IdentityPasswordHasher(IOptions<PasswordSettings> options)
    {
        _legacyHasher = new Sha512Encrypter(options.Value.AdditionalKey);
    }

    public string Hash(User user, string password) => _passwordHasher.HashPassword(user, password);

    public PasswordHashVerification Verify(User user, string hashedPassword, string providedPassword)
    {
        // Stored hashes predating the ASP.NET Core Identity hasher are not in its format, and
        // it rejects some of them by throwing rather than returning Failed. Those still have to
        // reach the legacy check below, so the throw is absorbed here.
        PasswordVerificationResult result;

        try
        {
            result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
        catch (FormatException)
        {
            result = PasswordVerificationResult.Failed;
        }
        catch (ArgumentException)
        {
            result = PasswordVerificationResult.Failed;
        }

        if (result is PasswordVerificationResult.Success)
            return PasswordHashVerification.Success;

        if (result is PasswordVerificationResult.SuccessRehashNeeded)
            return PasswordHashVerification.SuccessRehashNeeded;

        if (IsLegacySha512Hash(hashedPassword)
            && string.Equals(_legacyHasher.Encrypt(providedPassword), hashedPassword, StringComparison.Ordinal))
            return PasswordHashVerification.SuccessRehashNeeded;

        return PasswordHashVerification.Failed;
    }

    private static bool IsLegacySha512Hash(string hashedPassword)
    {
        return hashedPassword.Length == 128
            && hashedPassword.All(Uri.IsHexDigit);
    }
}

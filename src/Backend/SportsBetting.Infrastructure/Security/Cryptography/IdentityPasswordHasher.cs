using Microsoft.AspNetCore.Identity;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Security.Cryptography;

namespace SportsBetting.Infrastructure.Security.Cryptography;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string Hash(User user, string password) => _passwordHasher.HashPassword(user, password);

    public bool Verify(User user, string hashedPassword, string providedPassword)
    {
        try
        {
            return _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword)
                is not PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // A stored value that is not in the hasher's format is corrupt data, not a wrong
            // password. Answering "no" keeps that out of the login path as a 401 instead of a 500.
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

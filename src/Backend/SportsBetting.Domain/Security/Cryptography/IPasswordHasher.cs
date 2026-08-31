using SportsBetting.Domain.Entities;

namespace SportsBetting.Domain.Security.Cryptography;

public interface IPasswordHasher
{
    string Hash(User user, string password);

    PasswordHashVerification Verify(User user, string hashedPassword, string providedPassword);
}

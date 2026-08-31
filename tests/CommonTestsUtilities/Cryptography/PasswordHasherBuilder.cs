using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Infrastructure.Security.Cryptography;

namespace SportsBetting.Tests.Common.Cryptography;

public static class PasswordHasherBuilder
{
    public static IPasswordHasher Build() => new IdentityPasswordHasher();
}

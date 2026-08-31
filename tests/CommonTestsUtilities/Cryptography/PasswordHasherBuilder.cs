using Microsoft.Extensions.Options;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Infrastructure.Options;
using SportsBetting.Infrastructure.Security.Cryptography;

namespace SportsBetting.Tests.Common.Cryptography;

public static class PasswordHasherBuilder
{
    public static IPasswordHasher Build() => new IdentityPasswordHasher(Options.Create(new PasswordOptions
    {
        AdditionalKey = "abc1234"
    }));
}

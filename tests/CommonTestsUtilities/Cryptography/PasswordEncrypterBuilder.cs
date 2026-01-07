using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Infrastructure.Security.Cryptography;

namespace SportsBetting.Tests.Common.Cryptography;

public class PasswordEncrypterBuilder
{
    public static IPasswordEncrypter Build() => new Sha512Encrypter("abc1234");
}

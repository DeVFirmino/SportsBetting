using SportsBetting.Domain.Security.Tokens;
 using SportsBetting.Infrastructure.Security.Tokens.Access.Generator;

namespace SportsBetting.Tests.Common.Tokens;

public class JwtTokenGeneratorBuilder
{
    public static IAccessTokenGenerator Build() => new JwtTokenGenerator(expirationTimeMinutes: 5, signKey: "tttttttttttttttttttttttttttttttt");
}
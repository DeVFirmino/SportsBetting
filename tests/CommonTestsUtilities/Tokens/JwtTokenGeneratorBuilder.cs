using SportsBetting.Domain.Security.Tokens;
using Microsoft.Extensions.Options;
using SportsBetting.Infrastructure.Options;
using SportsBetting.Infrastructure.Security.Tokens.Access.Generator;

namespace SportsBetting.Tests.Common.Tokens;

public class JwtTokenGeneratorBuilder
{
    public static IAccessTokenGenerator Build() => new JwtTokenGenerator(Options.Create(new JwtOptions
    {
        SigningKey = "tttttttttttttttttttttttttttttttt",
        Issuer = "sportsbetting-tests",
        Audience = "sportsbetting-tests",
        ExpirationTimeMinutes = 5
    }));
}

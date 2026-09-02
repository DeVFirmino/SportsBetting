using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Infrastructure.Options;

namespace SportsBetting.Infrastructure.Security.Tokens.Access.Generator;

public sealed class JwtTokenGenerator : JwtTokenHandler, IAccessTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string Generate(Guid userIdentifier)
    {
        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userIdentifier.ToString()),
        ];

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_options.ExpirationTimeMinutes),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(
                SecurityKey(_options.SigningKey),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }


}

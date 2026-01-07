using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SportsBetting.Infrastructure.Security.Tokens.Access;

public abstract class JwtTokenHandler
{
    protected static SymmetricSecurityKey SecurityKey(string signKey)
    {
        var bytes = Encoding.UTF8.GetBytes(signKey);
        
        return new SymmetricSecurityKey(bytes);
    }
}
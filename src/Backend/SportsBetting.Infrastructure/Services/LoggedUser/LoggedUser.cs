using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Infrastructure.DataAccess;

namespace SportsBetting.Infrastructure.Services.LoggedUser;

public sealed class LoggedUser : ILoggedUser
{
    private readonly SportsBettingDbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;

    public LoggedUser(SportsBettingDbContext dbContext, ITokenProvider tokenProvider)
    {
        _dbContext = dbContext;
        _tokenProvider = tokenProvider;
    }

    public async Task<User> GetUserAsync(CancellationToken cancellationToken)
    {
        var token = _tokenProvider.Value();

        var tokenHandler = new JwtSecurityTokenHandler();
        
        var jwtSecurityToken = tokenHandler.ReadJwtToken(token);

        var identifier = jwtSecurityToken.Claims.First(c => c.Type == ClaimTypes.Sid).Value;
        
        var userIdentifier = Guid.Parse(identifier);

        return await _dbContext.Users.AsNoTracking()
            .FirstAsync(user => user.Active && user.UserIdentifier == userIdentifier, cancellationToken);
    }   
}

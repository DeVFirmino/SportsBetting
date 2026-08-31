using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Infrastructure.DataAccess;

namespace SportsBetting.Infrastructure.Services.LoggedUser;

public sealed class LoggedUser : ILoggedUser
{
    private readonly SportsBettingDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggedUser(SportsBettingDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<User> GetUserAsync(CancellationToken cancellationToken)
    {
        string identifier = _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Authenticated user claim is missing.");
        
        var userIdentifier = Guid.Parse(identifier);

        return await _dbContext.Users.AsNoTracking()
            .FirstAsync(user => user.Active && user.UserIdentifier == userIdentifier, cancellationToken);
    }   
}

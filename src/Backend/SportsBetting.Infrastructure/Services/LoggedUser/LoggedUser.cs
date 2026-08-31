using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions.ExceptionBase;
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

        // A token can outlive the account it names. Treating that as "not authenticated" keeps a
        // deactivated user out with a 401 instead of failing the request as a server error.
        return await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(user => user.Active && user.UserIdentifier == userIdentifier, cancellationToken)
            ?? throw new InvalidLoginException();
    }   
}

using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Infrastructure.DataAccess;

namespace SportsBetting.Infrastructure.DataAccess.Repositories;

public sealed class UserRepository : IUserReadOnlyRepository, IUserWriteOnlyRepository, IUserUpdateOnlyRepository
{
    private readonly SportsBettingDbContext _context;
    
    public UserRepository(SportsBettingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await _context.Users.AddAsync(user, cancellationToken);

    public async Task<bool> ExistsActiveUserWithEmailAsync(string email, CancellationToken cancellationToken)
    {
       return await _context.Users.AnyAsync(u => u.Email.Equals(email) && u.Active, cancellationToken);
    }

    public async Task<bool> ExistsActiveUserWithIdentifierAsync(
        Guid userIdentifier,
        CancellationToken cancellationToken)
    {
        return await _context
            .Users.AnyAsync(user => user
                .UserIdentifier.Equals(userIdentifier) && user
                .Active, cancellationToken);
        
    }
    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return await _context
            .Users
            .FirstOrDefaultAsync(u => u.Active && u.Email
                .Equals(email), cancellationToken);
     }

    public async Task<User> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _context.Users.FirstAsync(u => u.Id == id, cancellationToken);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
     }
}

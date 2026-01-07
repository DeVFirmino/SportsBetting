using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Infrastructure.DataAccess;

namespace SportsBetting.Infrastructure.DataAcess.Repositories;

public class UserRepository : IUserReadOnlyRepository, IUserWriteOnlyRepository, IUserUpdateOnlyRepository
{
    private readonly SportsBettingDbContext _context;
    
    public UserRepository(SportsBettingDbContext context)
    {
        _context = context;
    }

    public async Task Add(User user) => await _context.Users.AddAsync(user);

    public async Task<bool>ExistActiveUserWithEmail(string email)
    {
       return await _context.Users.AnyAsync(u => u.Email.Equals(email) && u.Active);
    }

    public async Task<bool> ExistActiveUserWithIdentifier(Guid userIdentifier)
    {
        return await _context
            .Users.AnyAsync(user => user
                .UserIdentifier.Equals(userIdentifier) && user
                .Active);
        
    }
    public async Task<User?> GetByEmailAndPassword(string email, string password)
    {
        return await _context
            .Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Active && u.Email
                .Equals(email) && u.Password
                .Equals(password));
     }

    public async Task<User> GetById(long id)
    {
        return await _context.Users.FirstAsync(u => u.Id == id);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
     }
}
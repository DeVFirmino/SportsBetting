using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Infrastructure.DataAccess;

namespace SportsBetting.Infrastructure.DataAcess.Repositories;

public class UserRepository : IUserReadOnlyRepository, IUserWriteOnlyRepository
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

}
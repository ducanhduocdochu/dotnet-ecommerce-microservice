using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;
    public UserRepository(AuthDbContext db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task AddAsync(User user) => await _db.Users.AddAsync(user);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}

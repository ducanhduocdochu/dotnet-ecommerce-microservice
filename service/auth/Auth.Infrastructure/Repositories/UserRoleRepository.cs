using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly AuthDbContext _context;

    public UserRoleRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserRole>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserRole?> GetByUserIdAndRoleIdAsync(Guid userId, Guid roleId)
    {
        return await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
    }

    public async Task AddAsync(UserRole userRole)
    {
        await _context.UserRoles.AddAsync(userRole);
    }

    public async Task RemoveAsync(UserRole userRole)
    {
        _context.UserRoles.Remove(userRole);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


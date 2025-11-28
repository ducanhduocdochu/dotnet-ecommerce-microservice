using Microsoft.EntityFrameworkCore;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Infrastructure.DB;

namespace User.Infrastructure.Repositories;

public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly UserDbContext _context;

    public UserPreferenceRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreference?> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task AddAsync(UserPreference preference)
    {
        await _context.UserPreferences.AddAsync(preference);
    }

    public async Task UpdateAsync(UserPreference preference)
    {
        _context.UserPreferences.Update(preference);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


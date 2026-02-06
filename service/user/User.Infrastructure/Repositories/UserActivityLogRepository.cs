using Microsoft.EntityFrameworkCore;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Infrastructure.DB;

namespace User.Infrastructure.Repositories;

public class UserActivityLogRepository : IUserActivityLogRepository
{
    private readonly UserDbContext _context;

    public UserActivityLogRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserActivityLog>> GetByUserIdAsync(Guid userId, int page, int pageSize, string? activityType = null)
    {
        var query = _context.UserActivityLogs.Where(a => a.UserId == userId);

        if (!string.IsNullOrEmpty(activityType))
        {
            query = query.Where(a => a.ActivityType == activityType);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId, string? activityType = null)
    {
        var query = _context.UserActivityLogs.Where(a => a.UserId == userId);

        if (!string.IsNullOrEmpty(activityType))
        {
            query = query.Where(a => a.ActivityType == activityType);
        }

        return await query.CountAsync();
    }

    public async Task AddAsync(UserActivityLog activityLog)
    {
        await _context.UserActivityLogs.AddAsync(activityLog);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


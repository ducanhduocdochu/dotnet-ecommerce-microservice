using User.Domain.Entities;

namespace User.Application.Interfaces;

public interface IUserActivityLogRepository
{
    Task<List<UserActivityLog>> GetByUserIdAsync(Guid userId, int page, int pageSize, string? activityType = null);
    Task<int> GetCountByUserIdAsync(Guid userId, string? activityType = null);
    Task AddAsync(UserActivityLog activityLog);
    Task SaveChangesAsync();
}


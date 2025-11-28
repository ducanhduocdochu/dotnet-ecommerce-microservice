using User.Domain.Entities;

namespace User.Application.Interfaces;

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserPreference preference);
    Task UpdateAsync(UserPreference preference);
    Task SaveChangesAsync();
}


using User.Domain.Entities;

namespace User.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId);
    Task<UserProfile?> GetByIdAsync(Guid id);
    Task AddAsync(UserProfile profile);
    Task UpdateAsync(UserProfile profile);
    Task SaveChangesAsync();
}


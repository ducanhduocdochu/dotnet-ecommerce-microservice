using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IUserRoleRepository
{
    Task<List<UserRole>> GetByUserIdAsync(Guid userId);
    Task<UserRole?> GetByUserIdAndRoleIdAsync(Guid userId, Guid roleId);
    Task AddAsync(UserRole userRole);
    Task RemoveAsync(UserRole userRole);
    Task SaveChangesAsync();
}


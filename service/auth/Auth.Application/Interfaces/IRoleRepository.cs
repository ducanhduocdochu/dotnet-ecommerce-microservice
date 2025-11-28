using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IRoleRepository
{
    Task<List<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByNameAsync(string name);
    Task AddAsync(Role role);
    Task UpdateAsync(Role role);
    Task RemoveAsync(Role role);
    Task SaveChangesAsync();
}


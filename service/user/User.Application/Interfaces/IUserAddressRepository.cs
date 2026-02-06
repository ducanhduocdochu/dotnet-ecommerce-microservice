using User.Domain.Entities;

namespace User.Application.Interfaces;

public interface IUserAddressRepository
{
    Task<List<UserAddress>> GetByUserIdAsync(Guid userId);
    Task<UserAddress?> GetByIdAsync(Guid id);
    Task<UserAddress?> GetDefaultByUserIdAsync(Guid userId);
    Task AddAsync(UserAddress address);
    Task UpdateAsync(UserAddress address);
    Task RemoveAsync(UserAddress address);
    Task SaveChangesAsync();
}


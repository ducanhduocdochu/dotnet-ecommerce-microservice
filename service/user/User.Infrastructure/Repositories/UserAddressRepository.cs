using Microsoft.EntityFrameworkCore;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Infrastructure.DB;

namespace User.Infrastructure.Repositories;

public class UserAddressRepository : IUserAddressRepository
{
    private readonly UserDbContext _context;

    public UserAddressRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserAddress>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserAddress?> GetByIdAsync(Guid id)
    {
        return await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<UserAddress?> GetDefaultByUserIdAsync(Guid userId)
    {
        return await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
    }

    public async Task AddAsync(UserAddress address)
    {
        await _context.UserAddresses.AddAsync(address);
    }

    public async Task UpdateAsync(UserAddress address)
    {
        _context.UserAddresses.Update(address);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(UserAddress address)
    {
        _context.UserAddresses.Remove(address);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


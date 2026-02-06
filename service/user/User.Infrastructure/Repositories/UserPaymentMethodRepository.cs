using Microsoft.EntityFrameworkCore;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Infrastructure.DB;

namespace User.Infrastructure.Repositories;

public class UserPaymentMethodRepository : IUserPaymentMethodRepository
{
    private readonly UserDbContext _context;

    public UserPaymentMethodRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserPaymentMethod>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserPaymentMethods
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserPaymentMethod?> GetByIdAsync(Guid id)
    {
        return await _context.UserPaymentMethods.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<UserPaymentMethod?> GetDefaultByUserIdAsync(Guid userId)
    {
        return await _context.UserPaymentMethods
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsDefault && p.IsActive);
    }

    public async Task AddAsync(UserPaymentMethod paymentMethod)
    {
        await _context.UserPaymentMethods.AddAsync(paymentMethod);
    }

    public async Task UpdateAsync(UserPaymentMethod paymentMethod)
    {
        _context.UserPaymentMethods.Update(paymentMethod);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(UserPaymentMethod paymentMethod)
    {
        _context.UserPaymentMethods.Remove(paymentMethod);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


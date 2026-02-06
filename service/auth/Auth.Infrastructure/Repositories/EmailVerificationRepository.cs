using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class EmailVerificationRepository : IEmailVerificationRepository
{
    private readonly AuthDbContext _context;

    public EmailVerificationRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerification?> GetByTokenAsync(string token)
    {   
        return await _context.EmailVerifications
            .FirstOrDefaultAsync(ev => ev.Token == token && !ev.IsUsed && ev.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<EmailVerification?> GetByUserIdAsync(Guid userId)
    {
        return await _context.EmailVerifications
            .Where(ev => ev.UserId == userId && !ev.IsUsed && ev.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(ev => ev.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(EmailVerification emailVerification)
    {
        await _context.EmailVerifications.AddAsync(emailVerification);
    }

    public async Task UpdateAsync(EmailVerification emailVerification)
    {
        _context.EmailVerifications.Update(emailVerification);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


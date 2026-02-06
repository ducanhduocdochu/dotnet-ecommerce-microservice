using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IEmailVerificationRepository
{
    Task<EmailVerification?> GetByTokenAsync(string token);
    Task<EmailVerification?> GetByUserIdAsync(Guid userId);
    Task AddAsync(EmailVerification emailVerification);
    Task UpdateAsync(EmailVerification emailVerification);
    Task SaveChangesAsync();
}


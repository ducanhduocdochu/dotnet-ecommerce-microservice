using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<List<RefreshToken>> GetByUserIdAsync(Guid userId);
    Task AddAsync(RefreshToken refreshToken);
    Task RevokeAsync(string token);
    Task RevokeAllByUserIdAsync(Guid userId);
    Task SaveChangesAsync();
}


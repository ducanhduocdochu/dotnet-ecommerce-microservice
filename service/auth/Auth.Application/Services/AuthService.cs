using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly string _jwtSecret;
    private readonly int _jwtExpiresMinutes;
    private readonly int _refreshTokenExpiresDays;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSecret = config["Jwt:Secret"] ?? "SuperSecretKey12345";
        _jwtExpiresMinutes = int.Parse(config["Jwt:ExpiresMinutes"] ?? "60");
        _refreshTokenExpiresDays = int.Parse(config["Jwt:RefreshTokenExpiresDays"] ?? "7");
    }

    public async Task<User?> RegisterAsync(string email, string password, string fullName)
    {
        var existing = await _userRepository.GetByEmailAsync(email);
        if (existing != null) return null;

        var hashed = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User(email, hashed, fullName);
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        return user;
    }

    public async Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || !user.IsActive) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        var accessToken = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return (accessToken, refreshToken);
    }

    public async Task<(string accessToken, string refreshToken)?> RefreshTokenAsync(string token)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
        if (refreshToken == null) return null;

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
        if (user == null || !user.IsActive) return null;

        // Revoke old token
        await _refreshTokenRepository.RevokeAsync(token);
        await _refreshTokenRepository.SaveChangesAsync();

        // Generate new tokens
        var newAccessToken = GenerateJwtToken(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);

        return (newAccessToken, newRefreshToken);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (token == null) return false;

        await _refreshTokenRepository.RevokeAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();
        return true;
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpiresMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                                                        SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiresDays)
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        return token;
    }
}

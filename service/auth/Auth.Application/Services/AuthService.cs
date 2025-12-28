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
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    private readonly EmailService _emailService;
    private readonly string _jwtSecret;
    private readonly int _jwtExpiresMinutes;
    private readonly int _refreshTokenExpiresDays;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IEmailVerificationRepository emailVerificationRepository,
        EmailService emailService,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _emailVerificationRepository = emailVerificationRepository;
        _emailService = emailService;
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

        // Assign default "Customer" role to new user
        var customerRole = await _roleRepository.GetByNameAsync("Customer");
        if (customerRole != null)
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = customerRole.Id
            };
            await _userRoleRepository.AddAsync(userRole);
            await _userRoleRepository.SaveChangesAsync();
        }

        return user;
    }

    public async Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || !user.IsActive) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        var accessToken = await GenerateJwtTokenAsync(user);
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
        var newAccessToken = await GenerateJwtTokenAsync(user);
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

    private async Task<string> GenerateJwtTokenAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        // Get user roles
        var userRoles = await _userRoleRepository.GetByUserIdAsync(user.Id);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Add role claims
        foreach (var userRole in userRoles)
        {
            var role = await _roleRepository.GetByIdAsync(userRole.RoleId);
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
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

    public async Task<bool> SendVerificationEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return false;

        // Generate verification token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");

        // Create email verification record
        var emailVerification = new EmailVerification(
            user.Id,
            token,
            DateTime.UtcNow.AddDays(1) // Expires in 24 hours
        );

        // Invalidate previous verification tokens for this user
        var existing = await _emailVerificationRepository.GetByUserIdAsync(user.Id);
        if (existing != null)
        {
            existing.MarkAsUsed();
            await _emailVerificationRepository.UpdateAsync(existing);
        }

        await _emailVerificationRepository.AddAsync(emailVerification);
        await _emailVerificationRepository.SaveChangesAsync();

        // Send email
        return await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, token);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var emailVerification = await _emailVerificationRepository.GetByTokenAsync(token);
        if (emailVerification == null) return false;


        var user = await _userRepository.GetByIdAsync(emailVerification.UserId);
        if (user == null) return false;

        // Mark token as used
        emailVerification.MarkAsUsed();
        await _emailVerificationRepository.UpdateAsync(emailVerification);

        // Activate user
        user.Activate();
        await _userRepository.SaveChangesAsync();

        return true;
    }
    
}

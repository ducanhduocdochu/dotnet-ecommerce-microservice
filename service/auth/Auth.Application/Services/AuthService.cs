using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Auth.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly string _jwtSecret;
    private readonly int _jwtExpiresMinutes;

    public AuthService(IUserRepository userRepository, IConfiguration config)
    {
        _userRepository = userRepository;
        _jwtSecret = config["Jwt:Secret"] ?? "SuperSecretKey12345";
        _jwtExpiresMinutes = int.Parse(config["Jwt:ExpiresMinutes"] ?? "60");
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

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        return GenerateJwtToken(user);
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
}

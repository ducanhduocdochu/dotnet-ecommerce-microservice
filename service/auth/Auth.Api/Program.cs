using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Infrastructure.Db;
using Auth.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnectParam")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var secret = builder.Configuration["Jwt:Secret"] ?? "SuperSecretKey12345";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(secret))
        };
    });

var app = builder.Build();

app.MapPost("/auth/register", async (AuthService authService, RegisterRequest request) =>
{
    var user = await authService.RegisterAsync(request.Email, request.Password, request.FullName);
    if (user == null) return Results.BadRequest(new { message = "Email already exists" });
    return Results.Ok(new { user.Id, user.Email, user.FullName });
});

app.MapPost("/auth/login", async (AuthService authService, LoginRequest request) =>
{
    var token = await authService.LoginAsync(request.Email, request.Password);
    if (token == null) return Results.Unauthorized();
    return Results.Ok(new { access_token = token });
});

app.MapGet("/auth/me", async (IUserRepository repo, HttpContext ctx) =>
{
    var userIdClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null) return Results.Unauthorized();
    var user = await repo.GetByIdAsync(Guid.Parse(userIdClaim));
    if (user == null) return Results.NotFound();
    return Results.Ok(new { user.Id, user.Email, user.FullName });
})
.RequireAuthorization();

app.Run();

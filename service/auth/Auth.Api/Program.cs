using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Domain.Entities;
using Auth.Infrastructure.Db;
using Auth.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
// upload image

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnectParam")));
// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();

// Register services
builder.Services.AddScoped<EmailService>();
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// ============================================
// Check all service connections on startup
// ============================================
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // 1. Check PostgreSQL connection
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    try
    {
        if (await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("✅ PostgreSQL connection successful!");
        }
        else
        {
            logger.LogError("❌ PostgreSQL connection failed!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ PostgreSQL connection error: {Message}", ex.Message);
    }
}

app.UseAuthentication();
app.UseAuthorization();

// POST /auth/register
app.MapPost("/auth/register", async (AuthService authService, RegisterRequest request) =>
{
    var user = await authService.RegisterAsync(request.Email, request.Password, request.FullName);
    if (user == null) return Results.BadRequest(new { message = "Email already exists" });
    return Results.Ok(new { user.Id, user.Email, user.FullName });
});

// POST /auth/login
app.MapPost("/auth/login", async (AuthService authService, LoginRequest request) =>
{
    var result = await authService.LoginAsync(request.Email, request.Password);
    if (result == null) return Results.Unauthorized();
    return Results.Ok(new { access_token = result.Value.accessToken, refresh_token = result.Value.refreshToken });
});

// POST /auth/refresh
app.MapPost("/auth/refresh", async (AuthService authService, RefreshTokenRequest request) =>
{
    var result = await authService.RefreshTokenAsync(request.RefreshToken);
    if (result == null) return Results.Unauthorized();
    return Results.Ok(new { access_token = result.Value.accessToken, refresh_token = result.Value.refreshToken });
});

// POST /auth/logout
app.MapPost("/auth/logout", async (AuthService authService, RefreshTokenRequest request) =>
{
    var success = await authService.LogoutAsync(request.RefreshToken);
    if (!success) return Results.BadRequest(new { message = "Invalid refresh token" });
    return Results.Ok(new { message = "Logged out successfully" });
});

// POST /auth/send-verification-email
app.MapPost("/auth/send-verification-email", async (AuthService authService, SendVerificationEmailRequest request) =>
{
    var success = await authService.SendVerificationEmailAsync(request.Email);
    if (!success) return Results.BadRequest(new { message = "User not found or failed to send email" });
    return Results.Ok(new { message = "Verification email sent successfully" });
});

// POST /auth/verify-email
app.MapPost("/auth/verify-email", async (AuthService authService, VerifyEmailRequest request) =>
{
    var success = await authService.VerifyEmailAsync(request.Token);
    if (!success) return Results.BadRequest(new { message = "Invalid or expired verification token" });
    return Results.Ok(new { message = "Email verified successfully. Your account has been activated." });
});

// GET /auth/me
app.MapGet("/auth/me", async (IUserRepository repo, HttpContext ctx) =>
{
    var userIdClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null) return Results.Unauthorized();
    var user = await repo.GetByIdAsync(Guid.Parse(userIdClaim));
    if (user == null) return Results.NotFound();
    return Results.Ok(new { user.Id, user.Email, user.FullName, user.IsActive, user.CreatedAt });
})
.RequireAuthorization();

// GET /auth/permissions
app.MapGet("/auth/permissions", async (
    IUserRepository userRepo,
    IUserRoleRepository userRoleRepo,
    IRoleRepository roleRepo,
    HttpContext ctx) =>
{
    var userIdClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null) return Results.Unauthorized();
    
    var userId = Guid.Parse(userIdClaim);
    var user = await userRepo.GetByIdAsync(userId);
    if (user == null) return Results.NotFound();

    var userRoles = await userRoleRepo.GetByUserIdAsync(userId);
    var roles = new List<object>();
    
    foreach (var ur in userRoles)
    {
        var role = await roleRepo.GetByIdAsync(ur.RoleId);
        if (role != null)
        {
            roles.Add(new { role.Id, role.Name, role.Description });
        }
    }

    return Results.Ok(new { user.Id, user.Email, Roles = roles });
})
.RequireAuthorization();

// GET /roles
app.MapGet("/roles", async (IRoleRepository roleRepo) =>
{
    var roles = await roleRepo.GetAllAsync();
    return Results.Ok(roles.Select(r => new { r.Id, r.Name, r.Description }));
})
.RequireAuthorization();

// POST /roles
app.MapPost("/roles", async (IRoleRepository roleRepo, RoleRequest request) =>
{
    var existing = await roleRepo.GetByNameAsync(request.Name);
    if (existing != null) return Results.BadRequest(new { message = "Role already exists" });

    var role = new Role(request.Name, request.Description);
    await roleRepo.AddAsync(role);
    await roleRepo.SaveChangesAsync();
    return Results.Created($"/roles/{role.Id}", new { role.Id, role.Name, role.Description });
})
.RequireAuthorization("AdminOnly");

// PUT /roles/{id}
app.MapPut("/roles/{id}", async (Guid id, IRoleRepository roleRepo, RoleRequest request) =>
{
    var role = await roleRepo.GetByIdAsync(id);
    if (role == null) return Results.NotFound(new { message = "Role not found" });

    var existing = await roleRepo.GetByNameAsync(request.Name);
    if (existing != null && existing.Id != id) return Results.BadRequest(new { message = "Role name already exists" });

    role.Update(request.Name, request.Description);
    await roleRepo.UpdateAsync(role);
    await roleRepo.SaveChangesAsync();
    return Results.Ok(new { role.Id, role.Name, role.Description });
})
.RequireAuthorization("AdminOnly");

// DELETE /roles/{id}
app.MapDelete("/roles/{id}", async (Guid id, IRoleRepository roleRepo) =>
{
    var role = await roleRepo.GetByIdAsync(id);
    if (role == null) return Results.NotFound(new { message = "Role not found" });

    await roleRepo.RemoveAsync(role);
    await roleRepo.SaveChangesAsync();
    return Results.Ok(new { message = "Role deleted successfully" });
})
.RequireAuthorization("AdminOnly");

// POST /user-roles
app.MapPost("/user-roles", async (
    IUserRepository userRepo,
    IRoleRepository roleRepo,
    IUserRoleRepository userRoleRepo,
    UserRoleRequest request) =>
{
    var user = await userRepo.GetByIdAsync(request.UserId);
    if (user == null) return Results.NotFound(new { message = "User not found" });

    var role = await roleRepo.GetByIdAsync(request.RoleId);
    if (role == null) return Results.NotFound(new { message = "Role not found" });

    var existing = await userRoleRepo.GetByUserIdAndRoleIdAsync(request.UserId, request.RoleId);
    if (existing != null) return Results.BadRequest(new { message = "User already has this role" });

    var userRole = new UserRole { UserId = request.UserId, RoleId = request.RoleId };
    await userRoleRepo.AddAsync(userRole);
    await userRoleRepo.SaveChangesAsync();
    return Results.Created($"/user-roles/{userRole.Id}", new { userRole.Id, userRole.UserId, userRole.RoleId });
})
.RequireAuthorization("AdminOnly");

// DELETE /user-roles/{userId}/{roleId}
app.MapDelete("/user-roles/{userId}/{roleId}", async (
    Guid userId,
    Guid roleId,
    IUserRoleRepository userRoleRepo) =>
{
    var userRole = await userRoleRepo.GetByUserIdAndRoleIdAsync(userId, roleId);
    if (userRole == null) return Results.NotFound(new { message = "User role not found" });

    await userRoleRepo.RemoveAsync(userRole);
    await userRoleRepo.SaveChangesAsync();
    return Results.Ok(new { message = "Role removed from user" });
})
.RequireAuthorization("AdminOnly");

app.Run();















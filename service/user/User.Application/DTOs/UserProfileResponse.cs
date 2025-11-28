namespace User.Application.DTOs;

public record UserProfileResponse(
    Guid Id,
    Guid UserId,
    string? Phone,
    DateTime? DateOfBirth,
    string? Gender,
    string? AvatarUrl,
    string? Bio,
    DateTime CreatedAt,
    DateTime UpdatedAt
);


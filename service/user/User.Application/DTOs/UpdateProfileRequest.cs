namespace User.Application.DTOs;

public record UpdateProfileRequest(
    string? Phone,
    DateTime? DateOfBirth,
    string? Gender,
    string? AvatarUrl,
    string? Bio
);


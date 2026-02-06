namespace Auth.Application.DTOs;

public record RoleResponse(
    Guid Id,
    string Name,
    string Description
);

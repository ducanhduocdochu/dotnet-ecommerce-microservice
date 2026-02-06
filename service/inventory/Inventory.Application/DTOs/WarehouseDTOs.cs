namespace Inventory.Application.DTOs;

// Requests
public record CreateWarehouseRequest(
    string Code,
    string Name,
    string? Address = null,
    string? City = null,
    string? Phone = null,
    string? ManagerName = null,
    bool IsDefault = false
);

public record UpdateWarehouseRequest(
    string? Name = null,
    string? Address = null,
    string? City = null,
    string? Phone = null,
    string? ManagerName = null,
    bool? IsActive = null,
    bool? IsDefault = null
);

// Responses
public record WarehouseResponse(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string? City,
    string? Phone,
    string? ManagerName,
    bool IsActive,
    bool IsDefault,
    DateTime CreatedAt
);


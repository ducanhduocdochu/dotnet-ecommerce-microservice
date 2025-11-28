namespace User.Application.DTOs;

public record UserAddressResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? StateProvince,
    string? PostalCode,
    string Country,
    bool IsDefault,
    string AddressType,
    DateTime CreatedAt,
    DateTime UpdatedAt
);


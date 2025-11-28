namespace User.Domain.Entities;

public class UserAddress
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string FullName { get; private set; }
    public string Phone { get; private set; }
    public string AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; }
    public string? StateProvince { get; private set; }
    public string? PostalCode { get; private set; }
    public string Country { get; private set; }
    public bool IsDefault { get; private set; } = false;
    public string AddressType { get; private set; } = "HOME"; // HOME, WORK, OTHER
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public UserAddress(
        Guid userId,
        string fullName,
        string phone,
        string addressLine1,
        string? addressLine2,
        string city,
        string? stateProvince,
        string? postalCode,
        string country,
        bool isDefault,
        string addressType)
    {
        UserId = userId;
        FullName = fullName;
        Phone = phone;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        StateProvince = stateProvince;
        PostalCode = postalCode;
        Country = country;
        IsDefault = isDefault;
        AddressType = addressType;
    }

    public void Update(
        string fullName,
        string phone,
        string addressLine1,
        string? addressLine2,
        string city,
        string? stateProvince,
        string? postalCode,
        string country,
        bool isDefault,
        string addressType)
    {
        FullName = fullName;
        Phone = phone;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        StateProvince = stateProvince;
        PostalCode = postalCode;
        Country = country;
        IsDefault = isDefault;
        AddressType = addressType;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault() => IsDefault = true;
    public void RemoveDefault() => IsDefault = false;
}


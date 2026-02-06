namespace Inventory.Domain.Entities;

public class Warehouse
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    
    // Address
    public string? Address { get; private set; }
    public string? Ward { get; private set; }
    public string? District { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; } = "Vietnam";
    
    // Contact
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? ManagerName { get; private set; }
    
    // Status
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; } = false;
    
    // Capacity
    public int? TotalCapacity { get; private set; }
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();

    private Warehouse() { }

    public Warehouse(string code, string name, string? city = null, bool isDefault = false)
    {
        Code = code;
        Name = name;
        City = city;
        IsDefault = isDefault;
    }

    public void Update(string? name, string? address, string? city, string? phone, string? managerName, bool? isActive, bool? isDefault)
    {
        if (name != null) Name = name;
        if (address != null) Address = address;
        if (city != null) City = city;
        if (phone != null) Phone = phone;
        if (managerName != null) ManagerName = managerName;
        if (isActive.HasValue) IsActive = isActive.Value;
        if (isDefault.HasValue) IsDefault = isDefault.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAddress(string? address, string? ward, string? district, string? city, string? country)
    {
        Address = address;
        Ward = ward;
        District = district;
        City = city;
        Country = country ?? "Vietnam";
        UpdatedAt = DateTime.UtcNow;
    }
}


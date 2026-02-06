namespace Payment.Domain.Entities;

public class PaymentGatewayConfig
{
    public Guid Id { get; private set; }
    public string GatewayCode { get; private set; } = string.Empty;
    public string GatewayName { get; private set; } = string.Empty;
    
    public bool IsActive { get; private set; } = true;
    public bool IsSandbox { get; private set; } = true;
    
    public string? ConfigData { get; private set; } // JSON
    public string[]? SupportedMethods { get; private set; }
    
    public string FeeType { get; private set; } = "PERCENT";
    public decimal FeePercent { get; private set; }
    public decimal FeeFixed { get; private set; }
    
    public decimal MinAmount { get; private set; } = 10000;
    public decimal MaxAmount { get; private set; } = 500000000;
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private PaymentGatewayConfig() { }
    
    public PaymentGatewayConfig(
        string gatewayCode,
        string gatewayName,
        string[]? supportedMethods,
        bool isActive = true,
        bool isSandbox = true)
    {
        Id = Guid.NewGuid();
        GatewayCode = gatewayCode;
        GatewayName = gatewayName;
        SupportedMethods = supportedMethods;
        IsActive = isActive;
        IsSandbox = isSandbox;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Update(bool? isActive, bool? isSandbox, string? configData, decimal? feePercent, decimal? feeFixed)
    {
        if (isActive.HasValue) IsActive = isActive.Value;
        if (isSandbox.HasValue) IsSandbox = isSandbox.Value;
        if (configData != null) ConfigData = configData;
        if (feePercent.HasValue) FeePercent = feePercent.Value;
        if (feeFixed.HasValue) FeeFixed = feeFixed.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}


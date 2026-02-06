namespace Payment.Domain.Entities;

public class PaymentMethodEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    
    public string Type { get; private set; } = string.Empty;
    public string? Provider { get; private set; }
    
    // Card Info
    public string? CardToken { get; private set; }
    public string? CardLast4 { get; private set; }
    public string? CardBrand { get; private set; }
    public int? CardExpMonth { get; private set; }
    public int? CardExpYear { get; private set; }
    public string? CardHolderName { get; private set; }
    
    // Bank Account Info
    public string? BankCode { get; private set; }
    public string? BankName { get; private set; }
    public string? AccountNumberMasked { get; private set; }
    public string? AccountHolderName { get; private set; }
    
    // E-Wallet Info
    public string? WalletId { get; private set; }
    public string? WalletPhone { get; private set; }
    
    // Metadata
    public bool IsDefault { get; private set; }
    public bool IsVerified { get; private set; }
    public string? Nickname { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private PaymentMethodEntity() { }
    
    public PaymentMethodEntity(
        Guid userId,
        string type,
        string? provider,
        string? nickname,
        bool isDefault = false)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Provider = provider;
        Nickname = nickname;
        IsDefault = isDefault;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetCardInfo(string? cardToken, string? cardLast4, string? cardBrand, 
        int? expMonth, int? expYear, string? holderName)
    {
        CardToken = cardToken;
        CardLast4 = cardLast4;
        CardBrand = cardBrand;
        CardExpMonth = expMonth;
        CardExpYear = expYear;
        CardHolderName = holderName;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetBankInfo(string? bankCode, string? bankName, string? accountNumberMasked, string? accountHolderName)
    {
        BankCode = bankCode;
        BankName = bankName;
        AccountNumberMasked = accountNumberMasked;
        AccountHolderName = accountHolderName;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetWalletInfo(string? walletId, string? walletPhone)
    {
        WalletId = walletId;
        WalletPhone = walletPhone;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Update(string? nickname, bool? isDefault)
    {
        if (nickname != null) Nickname = nickname;
        if (isDefault.HasValue) IsDefault = isDefault.Value;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Verify()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }
}


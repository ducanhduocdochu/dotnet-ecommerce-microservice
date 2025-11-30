namespace Order.Domain.Entities;

public class OrderEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string OrderNumber { get; private set; } = null!;
    
    // User Info (denormalized)
    public Guid UserId { get; private set; }
    public string? UserName { get; private set; }
    public string? UserEmail { get; private set; }
    public string? UserPhone { get; private set; }
    
    // Pricing
    public decimal Subtotal { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "VND";
    
    // Discount
    public string? DiscountCode { get; private set; }
    public Guid? DiscountId { get; private set; }
    
    // Status
    public string Status { get; private set; } = "PENDING";
    public string PaymentStatus { get; private set; } = "UNPAID";
    public string? PaymentMethod { get; private set; }
    
    // Shipping Address
    public string ShippingName { get; private set; } = null!;
    public string ShippingPhone { get; private set; } = null!;
    public string ShippingAddress { get; private set; } = null!;
    public string? ShippingWard { get; private set; }
    public string? ShippingDistrict { get; private set; }
    public string? ShippingCity { get; private set; }
    public string? ShippingCountry { get; private set; } = "Vietnam";
    public string? ShippingPostalCode { get; private set; }
    
    // Shipping Info
    public string? ShippingMethod { get; private set; }
    public string? ShippingCarrier { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime? EstimatedDelivery { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    
    // Notes
    public string? CustomerNote { get; private set; }
    public string? AdminNote { get; private set; }
    public string? CancelReason { get; private set; }
    
    // Payment Transaction
    public Guid? PaymentTransactionId { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    
    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<OrderPayment> Payments { get; set; } = new List<OrderPayment>();
    public ICollection<OrderRefund> Refunds { get; set; } = new List<OrderRefund>();

    private OrderEntity() { }

    public OrderEntity(
        Guid userId,
        string shippingName,
        string shippingPhone,
        string shippingAddress,
        string? paymentMethod = null,
        string? shippingMethod = null,
        string? customerNote = null,
        string? userName = null,
        string? userEmail = null,
        string? userPhone = null)
    {
        UserId = userId;
        OrderNumber = GenerateOrderNumber();
        ShippingName = shippingName;
        ShippingPhone = shippingPhone;
        ShippingAddress = shippingAddress;
        PaymentMethod = paymentMethod;
        ShippingMethod = shippingMethod;
        CustomerNote = customerNote;
        UserName = userName;
        UserEmail = userEmail;
        UserPhone = userPhone;
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    public void SetShippingAddress(string? ward, string? district, string? city, string? country, string? postalCode)
    {
        ShippingWard = ward;
        ShippingDistrict = district;
        ShippingCity = city;
        ShippingCountry = country ?? "Vietnam";
        ShippingPostalCode = postalCode;
    }

    public void CalculateTotals(decimal subtotal, decimal shippingFee, decimal discountAmount, decimal taxAmount)
    {
        Subtotal = subtotal;
        ShippingFee = shippingFee;
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        TotalAmount = subtotal + shippingFee - discountAmount + taxAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyDiscount(string? code, Guid? discountId, decimal amount)
    {
        DiscountCode = code;
        DiscountId = discountId;
        DiscountAmount = amount;
        TotalAmount = Subtotal + ShippingFee - amount + TaxAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDiscount(Guid? discountId, string? discountCode, decimal discountAmount)
    {
        DiscountId = discountId;
        DiscountCode = discountCode;
        DiscountAmount = discountAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPaymentTransaction(Guid? transactionId)
    {
        PaymentTransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmPayment()
    {
        Status = "CONFIRMED";
        PaymentStatus = "PAID";
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void FailPayment()
    {
        Status = "PAYMENT_FAILED";
        PaymentStatus = "FAILED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        Status = "CONFIRMED";
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Process()
    {
        Status = "PROCESSING";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Ship(string carrier, string trackingNumber, DateTime? estimatedDelivery = null)
    {
        Status = "SHIPPED";
        ShippingCarrier = carrier;
        TrackingNumber = trackingNumber;
        EstimatedDelivery = estimatedDelivery;
        ShippedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deliver()
    {
        Status = "DELIVERED";
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        Status = "CANCELLED";
        CancelReason = reason;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePaymentStatus(string status)
    {
        PaymentStatus = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAdminNote(string note)
    {
        AdminNote = note;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateUserInfo(string? userName, string? userEmail, string? userPhone)
    {
        UserName = userName;
        UserEmail = userEmail;
        UserPhone = userPhone;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanCancel() => Status is "PENDING" or "CONFIRMED";
    public bool CanConfirm() => Status == "PENDING";
    public bool CanProcess() => Status == "CONFIRMED";
    public bool CanShip() => Status == "PROCESSING";
    public bool CanDeliver() => Status == "SHIPPED";
}


namespace Order.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    
    // Product Info (denormalized)
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string? ProductSlug { get; private set; }
    public string? ProductImage { get; private set; }
    public string? ProductSku { get; private set; }
    
    // Variant Info
    public Guid? VariantId { get; private set; }
    public string? VariantName { get; private set; }
    public string? VariantOptions { get; private set; } // JSON: {"Color": "Red", "Size": "XL"}
    
    // Seller Info (denormalized)
    public Guid SellerId { get; private set; }
    public string? SellerName { get; private set; }
    
    // Pricing
    public decimal UnitPrice { get; private set; }
    public decimal? SalePrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }
    
    // Status
    public string Status { get; private set; } = "PENDING";
    
    // Inventory Reservation
    public Guid? ReservationId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public OrderEntity? Order { get; set; }

    private OrderItem() { }

    public OrderItem(
        Guid orderId,
        Guid productId,
        string productName,
        Guid sellerId,
        decimal unitPrice,
        int quantity,
        string? productSlug = null,
        string? productImage = null,
        string? productSku = null,
        Guid? variantId = null,
        string? variantName = null,
        string? variantOptions = null,
        string? sellerName = null,
        decimal? salePrice = null)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        ProductSlug = productSlug;
        ProductImage = productImage;
        ProductSku = productSku;
        VariantId = variantId;
        VariantName = variantName;
        VariantOptions = variantOptions;
        SellerId = sellerId;
        SellerName = sellerName;
        UnitPrice = unitPrice;
        SalePrice = salePrice;
        Quantity = quantity;
        TotalPrice = (salePrice ?? unitPrice) * quantity;
    }

    public void UpdateStatus(string status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSellerInfo(string? sellerName)
    {
        SellerName = sellerName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetReservationInfo(Guid? reservationId, Guid? warehouseId)
    {
        ReservationId = reservationId;
        WarehouseId = warehouseId;
        UpdatedAt = DateTime.UtcNow;
    }
}


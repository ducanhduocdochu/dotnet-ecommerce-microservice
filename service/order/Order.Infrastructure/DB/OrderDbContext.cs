using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Order.Domain.Entities;

namespace Order.Infrastructure.DB;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<OrderEntity> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
    public DbSet<OrderPayment> OrderPayments { get; set; }
    public DbSet<OrderRefund> OrderRefunds { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DateTime converter for PostgreSQL (UTC)
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }

        // Order
        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number").HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserName).HasColumnName("user_name").HasMaxLength(255);
            entity.Property(e => e.UserEmail).HasColumnName("user_email").HasMaxLength(255);
            entity.Property(e => e.UserPhone).HasColumnName("user_phone").HasMaxLength(50);
            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(15,2)");
            entity.Property(e => e.ShippingFee).HasColumnName("shipping_fee").HasColumnType("decimal(15,2)");
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(15,2)");
            entity.Property(e => e.TaxAmount).HasColumnName("tax_amount").HasColumnType("decimal(15,2)");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(15,2)");
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
            entity.Property(e => e.DiscountCode).HasColumnName("discount_code").HasMaxLength(50);
            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50);
            entity.Property(e => e.ShippingName).HasColumnName("shipping_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.ShippingPhone).HasColumnName("shipping_phone").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address").IsRequired();
            entity.Property(e => e.ShippingWard).HasColumnName("shipping_ward").HasMaxLength(100);
            entity.Property(e => e.ShippingDistrict).HasColumnName("shipping_district").HasMaxLength(100);
            entity.Property(e => e.ShippingCity).HasColumnName("shipping_city").HasMaxLength(100);
            entity.Property(e => e.ShippingCountry).HasColumnName("shipping_country").HasMaxLength(100);
            entity.Property(e => e.ShippingPostalCode).HasColumnName("shipping_postal_code").HasMaxLength(20);
            entity.Property(e => e.ShippingMethod).HasColumnName("shipping_method").HasMaxLength(50);
            entity.Property(e => e.ShippingCarrier).HasColumnName("shipping_carrier").HasMaxLength(100);
            entity.Property(e => e.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100);
            entity.Property(e => e.EstimatedDelivery).HasColumnName("estimated_delivery");
            entity.Property(e => e.ShippedAt).HasColumnName("shipped_at");
            entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(e => e.CustomerNote).HasColumnName("customer_note");
            entity.Property(e => e.AdminNote).HasColumnName("admin_note");
            entity.Property(e => e.CancelReason).HasColumnName("cancel_reason");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName).HasColumnName("product_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.ProductSlug).HasColumnName("product_slug").HasMaxLength(255);
            entity.Property(e => e.ProductImage).HasColumnName("product_image");
            entity.Property(e => e.ProductSku).HasColumnName("product_sku").HasMaxLength(100);
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.VariantName).HasColumnName("variant_name").HasMaxLength(255);
            entity.Property(e => e.VariantOptions).HasColumnName("variant_options");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.SellerName).HasColumnName("seller_name").HasMaxLength(255);
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.SalePrice).HasColumnName("sale_price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Order).WithMany(o => o.Items).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.SellerId);
        });

        // OrderStatusHistory
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("order_status_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PreviousStatus).HasColumnName("previous_status").HasMaxLength(50);
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.ChangedByName).HasColumnName("changed_by_name").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(e => e.Order).WithMany(o => o.StatusHistory).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OrderId);
        });

        // OrderPayment
        modelBuilder.Entity<OrderPayment>(entity =>
        {
            entity.ToTable("order_payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(15,2)");
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id").HasMaxLength(255);
            entity.Property(e => e.PaymentGateway).HasColumnName("payment_gateway").HasMaxLength(50);
            entity.Property(e => e.GatewayResponse).HasColumnName("gateway_response");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Order).WithMany(o => o.Payments).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OrderId);
        });

        // OrderRefund
        modelBuilder.Entity<OrderRefund>(entity =>
        {
            entity.ToTable("order_refunds");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(15,2)");
            entity.Property(e => e.Reason).HasColumnName("reason").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.RefundMethod).HasColumnName("refund_method").HasMaxLength(50);
            entity.Property(e => e.BankName).HasColumnName("bank_name").HasMaxLength(100);
            entity.Property(e => e.BankAccount).HasColumnName("bank_account").HasMaxLength(50);
            entity.Property(e => e.BankAccountName).HasColumnName("bank_account_name").HasMaxLength(255);
            entity.Property(e => e.ProcessedBy).HasColumnName("processed_by");
            entity.Property(e => e.ProcessedByName).HasColumnName("processed_by_name").HasMaxLength(255);
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.AdminNote).HasColumnName("admin_note");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Order).WithMany(o => o.Refunds).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OrderId);
        });

        // CartItem
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName).HasColumnName("product_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.ProductImage).HasColumnName("product_image");
            entity.Property(e => e.ProductPrice).HasColumnName("product_price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.VariantName).HasColumnName("variant_name").HasMaxLength(255);
            entity.Property(e => e.VariantPrice).HasColumnName("variant_price").HasColumnType("decimal(15,2)");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.SellerName).HasColumnName("seller_name").HasMaxLength(255);
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.ProductId, e.VariantId }).IsUnique();
        });
    }
}


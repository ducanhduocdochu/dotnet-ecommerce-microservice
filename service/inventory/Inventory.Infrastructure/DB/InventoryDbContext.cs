using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.DB;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<InventoryItem> Inventory { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }

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

        // Warehouse
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Ward).HasColumnName("ward").HasMaxLength(100);
            entity.Property(e => e.District).HasColumnName("district").HasMaxLength(100);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.ManagerName).HasColumnName("manager_name").HasMaxLength(255);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.TotalCapacity).HasColumnName("total_capacity");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // InventoryItem
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("inventory");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(100);
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ReservedQuantity).HasColumnName("reserved_quantity");
            entity.Ignore(e => e.AvailableQuantity); // Computed in code
            entity.Property(e => e.LowStockThreshold).HasColumnName("low_stock_threshold");
            entity.Property(e => e.ReorderPoint).HasColumnName("reorder_point");
            entity.Property(e => e.ReorderQuantity).HasColumnName("reorder_quantity");
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(100);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Warehouse).WithMany(w => w.InventoryItems).HasForeignKey(e => e.WarehouseId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.WarehouseId);
            entity.HasIndex(e => new { e.ProductId, e.VariantId, e.WarehouseId }).IsUnique();
        });

        // InventoryTransaction
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("inventory_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.QuantityChange).HasColumnName("quantity_change");
            entity.Property(e => e.QuantityBefore).HasColumnName("quantity_before");
            entity.Property(e => e.QuantityAfter).HasColumnName("quantity_after");
            entity.Property(e => e.ReferenceType).HasColumnName("reference_type").HasMaxLength(50);
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceCode).HasColumnName("reference_code").HasMaxLength(100);
            entity.Property(e => e.FromWarehouseId).HasColumnName("from_warehouse_id");
            entity.Property(e => e.ToWarehouseId).HasColumnName("to_warehouse_id");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(255);
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedByName).HasColumnName("created_by_name").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(e => e.Inventory).WithMany(i => i.Transactions).HasForeignKey(e => e.InventoryId);
            entity.HasIndex(e => e.InventoryId);
            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId });
        });

        // StockReservation
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("stock_reservations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number").HasMaxLength(50);
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.ExpiredAt).HasColumnName("expired_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.CommittedAt).HasColumnName("committed_at");
            entity.Property(e => e.ReleasedAt).HasColumnName("released_at");
            entity.HasOne(e => e.Inventory).WithMany(i => i.Reservations).HasForeignKey(e => e.InventoryId);
            entity.HasIndex(e => e.InventoryId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);
        });
    }
}


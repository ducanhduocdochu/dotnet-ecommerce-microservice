using Discount.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Discount.Infrastructure.DB;

public class DiscountDbContext : DbContext
{
    public DiscountDbContext(DbContextOptions<DiscountDbContext> options) : base(options)
    {
    }

    public DbSet<DiscountEntity> Discounts => Set<DiscountEntity>();
    public DbSet<DiscountProduct> DiscountProducts => Set<DiscountProduct>();
    public DbSet<DiscountCategory> DiscountCategories => Set<DiscountCategory>();
    public DbSet<DiscountUser> DiscountUsers => Set<DiscountUser>();
    public DbSet<DiscountUsage> DiscountUsages => Set<DiscountUsage>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionDiscount> PromotionDiscounts => Set<PromotionDiscount>();
    public DbSet<FlashSale> FlashSales => Set<FlashSale>();
    public DbSet<FlashSaleItem> FlashSaleItems => Set<FlashSaleItem>();

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

        // DiscountEntity
        modelBuilder.Entity<DiscountEntity>(entity =>
        {
            entity.ToTable("discounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Value).HasColumnName("value").HasPrecision(15, 2);
            entity.Property(e => e.MaxDiscountAmount).HasColumnName("max_discount_amount").HasPrecision(15, 2);
            entity.Property(e => e.MinOrderAmount).HasColumnName("min_order_amount").HasPrecision(15, 2);
            entity.Property(e => e.MinQuantity).HasColumnName("min_quantity");
            entity.Property(e => e.BuyQuantity).HasColumnName("buy_quantity");
            entity.Property(e => e.GetQuantity).HasColumnName("get_quantity");
            entity.Property(e => e.GetDiscountPercent).HasColumnName("get_discount_percent").HasPrecision(5, 2);
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.UsageLimitPerUser).HasColumnName("usage_limit_per_user");
            entity.Property(e => e.UsageCount).HasColumnName("usage_count");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsPublic).HasColumnName("is_public");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.IsStackable).HasColumnName("is_stackable");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedByName).HasColumnName("created_by_name").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });

        // DiscountProduct
        modelBuilder.Entity<DiscountProduct>(entity =>
        {
            entity.ToTable("discount_products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => new { e.DiscountId, e.ProductId }).IsUnique();
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(e => e.Discount)
                  .WithMany(d => d.DiscountProducts)
                  .HasForeignKey(e => e.DiscountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DiscountCategory
        modelBuilder.Entity<DiscountCategory>(entity =>
        {
            entity.ToTable("discount_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => new { e.DiscountId, e.CategoryId }).IsUnique();

            entity.HasOne(e => e.Discount)
                  .WithMany(d => d.DiscountCategories)
                  .HasForeignKey(e => e.DiscountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DiscountUser
        modelBuilder.Entity<DiscountUser>(entity =>
        {
            entity.ToTable("discount_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => new { e.DiscountId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Discount)
                  .WithMany(d => d.DiscountUsers)
                  .HasForeignKey(e => e.DiscountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DiscountUsage
        modelBuilder.Entity<DiscountUsage>(entity =>
        {
            entity.ToTable("discount_usages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number").HasMaxLength(50);
            entity.Property(e => e.OrderAmount).HasColumnName("order_amount").HasPrecision(15, 2);
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasPrecision(15, 2);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.DiscountId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OrderId);

            entity.HasOne(e => e.Discount)
                  .WithMany(d => d.DiscountUsages)
                  .HasForeignKey(e => e.DiscountId);
        });

        // Promotion
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("promotions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.BannerUrl).HasColumnName("banner_url");
            entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedByName).HasColumnName("created_by_name").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate });
        });

        // PromotionDiscount
        modelBuilder.Entity<PromotionDiscount>(entity =>
        {
            entity.ToTable("promotion_discounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.DiscountId).HasColumnName("discount_id");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => new { e.PromotionId, e.DiscountId }).IsUnique();

            entity.HasOne(e => e.Promotion)
                  .WithMany(p => p.PromotionDiscounts)
                  .HasForeignKey(e => e.PromotionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Discount)
                  .WithMany()
                  .HasForeignKey(e => e.DiscountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FlashSale
        modelBuilder.Entity<FlashSale>(entity =>
        {
            entity.ToTable("flash_sales");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // FlashSaleItem
        modelBuilder.Entity<FlashSaleItem>(entity =>
        {
            entity.ToTable("flash_sale_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FlashSaleId).HasColumnName("flash_sale_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.OriginalPrice).HasColumnName("original_price").HasPrecision(15, 2);
            entity.Property(e => e.SalePrice).HasColumnName("sale_price").HasPrecision(15, 2);
            entity.Property(e => e.DiscountPercent).HasColumnName("discount_percent").HasPrecision(5, 2);
            entity.Property(e => e.QuantityLimit).HasColumnName("quantity_limit");
            entity.Property(e => e.QuantitySold).HasColumnName("quantity_sold");
            entity.Property(e => e.LimitPerUser).HasColumnName("limit_per_user");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => new { e.FlashSaleId, e.ProductId, e.VariantId }).IsUnique();
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(e => e.FlashSale)
                  .WithMany(f => f.FlashSaleItems)
                  .HasForeignKey(e => e.FlashSaleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}


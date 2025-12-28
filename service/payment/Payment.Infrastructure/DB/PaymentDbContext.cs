using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.DB;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<RefundTransaction> RefundTransactions => Set<RefundTransaction>();
    public DbSet<PaymentMethodEntity> PaymentMethods => Set<PaymentMethodEntity>();
    public DbSet<PaymentLog> PaymentLogs => Set<PaymentLog>();
    public DbSet<PaymentGatewayConfig> PaymentGatewayConfigs => Set<PaymentGatewayConfig>();

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

        // PaymentTransaction
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("payment_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TransactionCode).HasColumnName("transaction_code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number").HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserEmail).HasColumnName("user_email").HasMaxLength(255);
            entity.Property(e => e.UserPhone).HasColumnName("user_phone").HasMaxLength(50);
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(15, 2);
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50);
            entity.Property(e => e.PaymentGateway).HasColumnName("payment_gateway").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.GatewayTransactionId).HasColumnName("gateway_transaction_id").HasMaxLength(255);
            entity.Property(e => e.GatewayResponseCode).HasColumnName("gateway_response_code").HasMaxLength(50);
            entity.Property(e => e.GatewayResponseMessage).HasColumnName("gateway_response_message");
            entity.Property(e => e.GatewayResponseData).HasColumnName("gateway_response_data").HasColumnType("jsonb");
            entity.Property(e => e.PaymentUrl).HasColumnName("payment_url");
            entity.Property(e => e.ReturnUrl).HasColumnName("return_url");
            entity.Property(e => e.CallbackUrl).HasColumnName("callback_url");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.ExpiredAt).HasColumnName("expired_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.Description).HasColumnName("description");

            entity.HasIndex(e => e.TransactionCode).IsUnique();
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // RefundTransaction
        modelBuilder.Entity<RefundTransaction>(entity =>
        {
            entity.ToTable("refund_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RefundCode).HasColumnName("refund_code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PaymentTransactionId).HasColumnName("payment_transaction_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderRefundId).HasColumnName("order_refund_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(15, 2);
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
            entity.Property(e => e.RefundMethod).HasColumnName("refund_method").HasMaxLength(50);
            entity.Property(e => e.BankCode).HasColumnName("bank_code").HasMaxLength(50);
            entity.Property(e => e.BankName).HasColumnName("bank_name").HasMaxLength(255);
            entity.Property(e => e.BankAccount).HasColumnName("bank_account").HasMaxLength(50);
            entity.Property(e => e.BankAccountName).HasColumnName("bank_account_name").HasMaxLength(255);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.GatewayRefundId).HasColumnName("gateway_refund_id").HasMaxLength(255);
            entity.Property(e => e.GatewayResponseCode).HasColumnName("gateway_response_code").HasMaxLength(50);
            entity.Property(e => e.GatewayResponseMessage).HasColumnName("gateway_response_message");
            entity.Property(e => e.GatewayResponseData).HasColumnName("gateway_response_data").HasColumnType("jsonb");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.AdminNote).HasColumnName("admin_note");
            entity.Property(e => e.ProcessedBy).HasColumnName("processed_by");
            entity.Property(e => e.ProcessedByName).HasColumnName("processed_by_name").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");

            entity.HasIndex(e => e.RefundCode).IsUnique();
            entity.HasIndex(e => e.PaymentTransactionId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.PaymentTransaction)
                  .WithMany(p => p.Refunds)
                  .HasForeignKey(e => e.PaymentTransactionId);
        });

        // PaymentMethodEntity
        modelBuilder.Entity<PaymentMethodEntity>(entity =>
        {
            entity.ToTable("payment_methods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50);
            entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(50);
            entity.Property(e => e.CardToken).HasColumnName("card_token").HasMaxLength(255);
            entity.Property(e => e.CardLast4).HasColumnName("card_last4").HasMaxLength(4);
            entity.Property(e => e.CardBrand).HasColumnName("card_brand").HasMaxLength(50);
            entity.Property(e => e.CardExpMonth).HasColumnName("card_exp_month");
            entity.Property(e => e.CardExpYear).HasColumnName("card_exp_year");
            entity.Property(e => e.CardHolderName).HasColumnName("card_holder_name").HasMaxLength(255);
            entity.Property(e => e.BankCode).HasColumnName("bank_code").HasMaxLength(50);
            entity.Property(e => e.BankName).HasColumnName("bank_name").HasMaxLength(255);
            entity.Property(e => e.AccountNumberMasked).HasColumnName("account_number_masked").HasMaxLength(50);
            entity.Property(e => e.AccountHolderName).HasColumnName("account_holder_name").HasMaxLength(255);
            entity.Property(e => e.WalletId).HasColumnName("wallet_id").HasMaxLength(255);
            entity.Property(e => e.WalletPhone).HasColumnName("wallet_phone").HasMaxLength(50);
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.IsVerified).HasColumnName("is_verified");
            entity.Property(e => e.Nickname).HasColumnName("nickname").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId);
        });

        // PaymentLog
        modelBuilder.Entity<PaymentLog>(entity =>
        {
            entity.ToTable("payment_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.RefundId).HasColumnName("refund_id");
            entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.RequestData).HasColumnName("request_data").HasColumnType("jsonb");
            entity.Property(e => e.ResponseData).HasColumnName("response_data").HasColumnType("jsonb");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.PaymentTransaction)
                  .WithMany(p => p.Logs)
                  .HasForeignKey(e => e.TransactionId);

            entity.HasOne(e => e.RefundTransaction)
                  .WithMany()
                  .HasForeignKey(e => e.RefundId);
        });

        // PaymentGatewayConfig
        modelBuilder.Entity<PaymentGatewayConfig>(entity =>
        {
            entity.ToTable("payment_gateway_configs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GatewayCode).HasColumnName("gateway_code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.GatewayName).HasColumnName("gateway_name").HasMaxLength(100);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsSandbox).HasColumnName("is_sandbox");
            entity.Property(e => e.ConfigData).HasColumnName("config_data").HasColumnType("jsonb");
            entity.Property(e => e.SupportedMethods).HasColumnName("supported_methods");
            entity.Property(e => e.FeeType).HasColumnName("fee_type").HasMaxLength(20);
            entity.Property(e => e.FeePercent).HasColumnName("fee_percent").HasPrecision(5, 2);
            entity.Property(e => e.FeeFixed).HasColumnName("fee_fixed").HasPrecision(15, 2);
            entity.Property(e => e.MinAmount).HasColumnName("min_amount").HasPrecision(15, 2);
            entity.Property(e => e.MaxAmount).HasColumnName("max_amount").HasPrecision(15, 2);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.GatewayCode).IsUnique();
        });
    }
}


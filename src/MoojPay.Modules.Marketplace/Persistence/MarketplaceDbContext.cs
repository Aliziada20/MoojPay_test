using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MoojPay.Domain.Entities;
using MoojPay.Domain.Enums;

namespace MoojPay.Modules.Marketplace.Persistence;

/// <summary>
/// EF Core context mapped to a schema created and versioned entirely by raw SQL scripts (see
/// <see cref="SqlMigrations.SqlMigrationRunner"/> and <c>Migrations/Sql/*.sql</c>), per the
/// accepted BuildSpec's "EF Core 10+ with raw versioned SQL migrations (not ORM-generated)"
/// stack decision. No <c>dotnet ef migrations</c> scaffolding is used or should be added.
/// </summary>
public sealed class MarketplaceDbContext(DbContextOptions<MarketplaceDbContext> options) : DbContext(options)
{
    public DbSet<MarketplaceOrder> MarketplaceOrders => Set<MarketplaceOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketplaceOrder>(builder =>
        {
            builder.ToTable("marketplace_orders");
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Id).HasColumnName("id");
            builder.Property(o => o.Status)
                .HasColumnName("status")
                .HasConversion<string>();
            builder.Property(o => o.TotalAmount).HasColumnName("total_amount");
            builder.Property(o => o.Currency).HasColumnName("currency");
            builder.Property(o => o.CreatedAt).HasColumnName("created_at");
            builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
            builder.Property(o => o.ZapsPurchaseReference).HasColumnName("zaps_purchase_reference");
            builder.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key");
            builder.HasIndex(o => o.IdempotencyKey).IsUnique();

            var itemsProperty = builder.Property(o => o.Items)
                .HasColumnName("items")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<MarketplaceOrderItem>>(v, JsonOptions) ?? new List<MarketplaceOrderItem>());

            itemsProperty.Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<MarketplaceOrderItem>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                v => v.ToList()));
        });
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

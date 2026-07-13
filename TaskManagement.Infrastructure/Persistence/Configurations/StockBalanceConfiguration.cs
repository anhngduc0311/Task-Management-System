using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
    {
        public void Configure(EntityTypeBuilder<StockBalance> builder)
        {
            builder.ToTable("StockBalances");

            builder.HasKey(sb => sb.Id);

            builder.Property(sb => sb.Quantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(sb => sb.LastUpdatedAt)
                .IsRequired();

            builder.HasOne(sb => sb.Warehouse)
                .WithMany()
                .HasForeignKey(sb => sb.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sb => sb.Product)
                .WithMany()
                .HasForeignKey(sb => sb.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sb => sb.ProductVariant)
                .WithMany()
                .HasForeignKey(sb => sb.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Filtered index on WarehouseId (common filter)
            builder.HasIndex(sb => sb.WarehouseId)
                .HasDatabaseName("IX_StockBalances_WarehouseId");

            // Filtered unique indexes for combination to prevent duplicates
            builder.HasIndex(sb => new { sb.WarehouseId, sb.ProductId, sb.ProductVariantId })
                .HasDatabaseName("IX_StockBalances_Unique_WithVariant")
                .HasFilter("[ProductVariantId] IS NOT NULL")
                .IsUnique();

            builder.HasIndex(sb => new { sb.WarehouseId, sb.ProductId })
                .HasDatabaseName("IX_StockBalances_Unique_NoVariant")
                .HasFilter("[ProductVariantId] IS NULL")
                .IsUnique();

            builder.HasData(
                new StockBalance
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000011"),
                    WarehouseId = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000001"),
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000011"),
                    Quantity = 50m,
                    LastUpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new StockBalance
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000012"),
                    WarehouseId = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000001"),
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000012"),
                    Quantity = 30m,
                    LastUpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new StockBalance
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000021"),
                    WarehouseId = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000002"),
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000021"),
                    Quantity = 1200m,
                    LastUpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new StockBalance
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000031"),
                    WarehouseId = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000003"),
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000031"),
                    Quantity = 500m,
                    LastUpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new StockBalance
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000041"),
                    WarehouseId = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"),
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000041"),
                    Quantity = 150m,
                    LastUpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new StockBalance
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000042"),
                    WarehouseId = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"),
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000042"),
                    Quantity = 200m,
                    LastUpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new StockBalance
                {
                    Id = Guid.Parse("b0000000-0000-0000-0000-000000000043"),
                    WarehouseId = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"),
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000043"),
                    Quantity = 100m,
                    LastUpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

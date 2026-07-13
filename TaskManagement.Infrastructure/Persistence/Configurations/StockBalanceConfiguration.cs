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
        }
    }
}

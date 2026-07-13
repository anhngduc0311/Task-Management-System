using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            builder.ToTable("StockMovements");

            builder.HasKey(sm => sm.Id);

            builder.Property(sm => sm.Quantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(sm => sm.MovementType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(sm => sm.ReferenceId);

            builder.Property(sm => sm.ReferenceNo)
                .HasMaxLength(100);

            builder.Property(sm => sm.CreatedAt)
                .IsRequired();

            builder.HasOne(sm => sm.Warehouse)
                .WithMany()
                .HasForeignKey(sm => sm.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sm => sm.Product)
                .WithMany()
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sm => sm.ProductVariant)
                .WithMany()
                .HasForeignKey(sm => sm.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(sm => sm.WarehouseId)
                .HasDatabaseName("IX_StockMovements_WarehouseId");

            builder.HasIndex(sm => sm.MovementType)
                .HasDatabaseName("IX_StockMovements_MovementType");

            builder.HasIndex(sm => new { sm.ProductId, sm.ProductVariantId })
                .HasDatabaseName("IX_StockMovements_ProductVariant");
        }
    }
}

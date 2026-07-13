using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ImportReceiptConfiguration : IEntityTypeConfiguration<ImportReceipt>
    {
        public void Configure(EntityTypeBuilder<ImportReceipt> builder)
        {
            builder.ToTable("ImportReceipts");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.ReceiptNo)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(r => r.Description)
                .HasMaxLength(1000);

            builder.Property(r => r.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired();

            builder.Property(r => r.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            builder.HasOne(r => r.Supplier)
                .WithMany()
                .HasForeignKey(r => r.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Warehouse)
                .WithMany()
                .HasForeignKey(r => r.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.ReceiptNo)
                .IsUnique();

            builder.HasIndex(r => r.WarehouseId);
            builder.HasIndex(r => r.Status);
        }
    }
}

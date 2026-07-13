using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ExportReceiptConfiguration : IEntityTypeConfiguration<ExportReceipt>
    {
        public void Configure(EntityTypeBuilder<ExportReceipt> builder)
        {
            builder.ToTable("ExportReceipts");

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

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired();

            builder.Property(r => r.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

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

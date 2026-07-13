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

            builder.HasData(
                new ImportReceipt
                {
                    Id = Guid.Parse("db000000-0000-0000-0000-000000000001"),
                    ReceiptNo = "IMP202607130001",
                    SupplierId = Guid.Parse("a0000000-0000-0000-0000-000000000001"), // Synnex FPT
                    WarehouseId = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"), // Main Warehouse
                    Status = TaskManagement.Domain.Enums.ReceiptStatus.Confirmed,
                    Description = "Nhập kho lô hàng Laptop Dell XPS 13 & Áo thun Polo Uniqlo phục vụ kinh doanh.",
                    TotalAmount = 2282000000m,
                    CreatedById = Guid.Parse("8a4b4ef9-7ec7-4dbb-8fb6-82ff4b4ab456"), // admin
                    CreatedAt = new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

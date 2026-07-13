using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class TransferReceiptConfiguration : IEntityTypeConfiguration<TransferReceipt>
    {
        public void Configure(EntityTypeBuilder<TransferReceipt> builder)
        {
            builder.ToTable("TransferReceipts", t => t.HasCheckConstraint("CK_TransferReceipts_FromDifferentTo", "[FromWarehouseId] <> [ToWarehouseId]"));

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

            builder.HasOne(r => r.FromWarehouse)
                .WithMany()
                .HasForeignKey(r => r.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ToWarehouse)
                .WithMany()
                .HasForeignKey(r => r.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.ReceiptNo)
                .IsUnique();

            builder.HasIndex(r => r.FromWarehouseId);
            builder.HasIndex(r => r.ToWarehouseId);
            builder.HasIndex(r => r.Status);
        }
    }
}

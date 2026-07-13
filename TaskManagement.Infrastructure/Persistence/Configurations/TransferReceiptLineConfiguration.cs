using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class TransferReceiptLineConfiguration : IEntityTypeConfiguration<TransferReceiptLine>
    {
        public void Configure(EntityTypeBuilder<TransferReceiptLine> builder)
        {
            builder.ToTable("TransferReceiptLines");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Quantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(l => l.BaseQuantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(l => l.ConversionRate)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(l => l.CreatedAt)
                .IsRequired();

            builder.HasOne(l => l.TransferReceipt)
                .WithMany(r => r.Lines)
                .HasForeignKey(l => l.TransferReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.ProductVariant)
                .WithMany()
                .HasForeignKey(l => l.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.Unit)
                .WithMany()
                .HasForeignKey(l => l.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ImportReceiptLineConfiguration : IEntityTypeConfiguration<ImportReceiptLine>
    {
        public void Configure(EntityTypeBuilder<ImportReceiptLine> builder)
        {
            builder.ToTable("ImportReceiptLines");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Quantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(l => l.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(l => l.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(l => l.BaseQuantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(l => l.ConversionRate)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(l => l.CreatedAt)
                .IsRequired();

            builder.HasOne(l => l.ImportReceipt)
                .WithMany(r => r.Lines)
                .HasForeignKey(l => l.ImportReceiptId)
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

            builder.HasData(
                new ImportReceiptLine
                {
                    Id = Guid.Parse("db000000-0000-0000-0000-000000000011"),
                    ImportReceiptId = Guid.Parse("db000000-0000-0000-0000-000000000001"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000001"), // Dell XPS 13
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000011"), // i7
                    Quantity = 50m,
                    UnitId = Guid.Parse("f11e11a1-1111-1111-1111-111111111111"), // CAI
                    UnitPrice = 45000000m,
                    Amount = 2250000000m,
                    BaseQuantity = 50m,
                    ConversionRate = 1m,
                    CreatedAt = new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc)
                },
                new ImportReceiptLine
                {
                    Id = Guid.Parse("db000000-0000-0000-0000-000000000012"),
                    ImportReceiptId = Guid.Parse("db000000-0000-0000-0000-000000000001"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"), // Polo Uniqlo
                    ProductVariantId = Guid.Parse("fa000000-0000-0000-0000-000000000041"), // Size M
                    Quantity = 65m,
                    UnitId = Guid.Parse("f11e11a1-1111-1111-1111-111111111111"), // CAI
                    UnitPrice = 492307.6923m,
                    Amount = 32000000m,
                    BaseQuantity = 65m,
                    ConversionRate = 1m,
                    CreatedAt = new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

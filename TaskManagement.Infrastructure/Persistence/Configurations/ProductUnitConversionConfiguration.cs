using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductUnitConversionConfiguration : IEntityTypeConfiguration<ProductUnitConversion>
    {
        public void Configure(EntityTypeBuilder<ProductUnitConversion> builder)
        {
            builder.ToTable("ProductUnitConversions", t => t.HasCheckConstraint("CK_ProductUnitConversions_ConversionRate", "[ConversionRate] > 0"));

            builder.HasKey(uc => uc.Id);

            builder.Property(uc => uc.ConversionRate)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(uc => uc.CreatedAt)
                .IsRequired();

            builder.Property(uc => uc.UpdatedAt)
                .IsRequired();

            builder.HasOne(uc => uc.Product)
                .WithMany(p => p.UnitConversions)
                .HasForeignKey(uc => uc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uc => uc.FromUnit)
                .WithMany()
                .HasForeignKey(uc => uc.FromUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(uc => uc.ToUnit)
                .WithMany()
                .HasForeignKey(uc => uc.ToUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(uc => new { uc.ProductId, uc.FromUnitId, uc.ToUnitId })
                .IsUnique();

            builder.HasData(
                new ProductUnitConversion
                {
                    Id = Guid.Parse("fb000000-0000-0000-0000-000000000021"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000002"), // Vinamilk Milk
                    FromUnitId = Guid.Parse("f11e11a1-3333-3333-3333-333333333333"), // THUNG
                    ToUnitId = Guid.Parse("f11e11a1-2222-2222-2222-222222222222"), // HOP
                    ConversionRate = 48m,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProductUnitConversion
                {
                    Id = Guid.Parse("fb000000-0000-0000-0000-000000000031"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000003"), // Bút bi TL-027
                    FromUnitId = Guid.Parse("f11e11a1-2222-2222-2222-222222222222"), // HOP
                    ToUnitId = Guid.Parse("f11e11a1-1111-1111-1111-111111111111"), // CAI
                    ConversionRate = 20m,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

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
        }
    }
}

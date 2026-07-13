using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductVariantAttributeValueConfiguration : IEntityTypeConfiguration<ProductVariantAttributeValue>
    {
        public void Configure(EntityTypeBuilder<ProductVariantAttributeValue> builder)
        {
            builder.ToTable("ProductVariantAttributeValues");

            builder.HasKey(vav => new { vav.ProductVariantId, vav.ProductAttributeValueId });

            builder.HasOne(vav => vav.ProductVariant)
                .WithMany(v => v.VariantAttributeValues)
                .HasForeignKey(vav => vav.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(vav => vav.ProductAttributeValue)
                .WithMany(val => val.VariantAttributeValues)
                .HasForeignKey(vav => vav.ProductAttributeValueId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

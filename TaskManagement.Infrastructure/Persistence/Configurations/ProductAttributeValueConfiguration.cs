using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
    {
        public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
        {
            builder.ToTable("ProductAttributeValues");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Value)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(v => v.DisplayOrder)
                .IsRequired();

            builder.HasOne(v => v.AttributeGroup)
                .WithMany(g => g.AttributeValues)
                .HasForeignKey(v => v.AttributeGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(v => new { v.AttributeGroupId, v.Value })
                .IsUnique();
        }
    }
}

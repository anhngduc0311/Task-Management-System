using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductAttributeGroupConfiguration : IEntityTypeConfiguration<ProductAttributeGroup>
    {
        public void Configure(EntityTypeBuilder<ProductAttributeGroup> builder)
        {
            builder.ToTable("ProductAttributeGroups");

            builder.HasKey(g => g.Id);

            builder.Property(g => g.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(g => g.DisplayOrder)
                .IsRequired();

            builder.HasOne(g => g.Product)
                .WithMany(p => p.AttributeGroups)
                .HasForeignKey(g => g.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(g => new { g.ProductId, g.Name })
                .IsUnique();
        }
    }
}

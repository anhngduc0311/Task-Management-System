using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.ToTable("ProductImages");

            builder.HasKey(pi => pi.Id);

            builder.Property(pi => pi.FileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(pi => pi.StorageKey)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(pi => pi.Url)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(pi => pi.IsPrimary)
                .IsRequired();

            builder.Property(pi => pi.DisplayOrder)
                .IsRequired();

            builder.Property(pi => pi.CreatedAt)
                .IsRequired();

            builder.HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

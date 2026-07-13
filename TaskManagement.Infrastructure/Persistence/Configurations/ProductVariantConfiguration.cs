using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.ToTable("ProductVariants");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.SKU)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(v => v.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(v => v.ImageUrl)
                .HasMaxLength(2000);

            builder.Property(v => v.IsDeleted)
                .IsRequired();

            builder.Property(v => v.CreatedAt)
                .IsRequired();

            builder.Property(v => v.UpdatedAt)
                .IsRequired();

            builder.Property(v => v.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            builder.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(v => !v.IsDeleted);

            builder.HasIndex(v => v.SKU)
                .HasDatabaseName("IX_ProductVariants_SKU")
                .HasFilter("[IsDeleted] = 0")
                .IsUnique();

            builder.HasIndex(v => v.ProductId)
                .HasDatabaseName("IX_ProductVariants_ProductId")
                .HasFilter("[IsDeleted] = 0");
        }
    }
}

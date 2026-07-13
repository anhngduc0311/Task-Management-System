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

            builder.HasData(
                new ProductVariant
                {
                    Id = Guid.Parse("fa000000-0000-0000-0000-000000000011"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000001"),
                    SKU = "DELL-XPS13-I7",
                    Price = 45000000m,
                    ImageUrl = null,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProductVariant
                {
                    Id = Guid.Parse("fa000000-0000-0000-0000-000000000012"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000001"),
                    SKU = "DELL-XPS13-I9",
                    Price = 55000000m,
                    ImageUrl = null,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProductVariant
                {
                    Id = Guid.Parse("fa000000-0000-0000-0000-000000000021"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000002"),
                    SKU = "VNM-MILK-180",
                    Price = 32000m,
                    ImageUrl = null,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProductVariant
                {
                    Id = Guid.Parse("fa000000-0000-0000-0000-000000000031"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000003"),
                    SKU = "TL-027-BLUE",
                    Price = 4000m,
                    ImageUrl = null,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProductVariant
                {
                    Id = Guid.Parse("fa000000-0000-0000-0000-000000000041"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"),
                    SKU = "UQ-POLO-M",
                    Price = 490000m,
                    ImageUrl = null,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProductVariant
                {
                    Id = Guid.Parse("fa000000-0000-0000-0000-000000000042"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"),
                    SKU = "UQ-POLO-L",
                    Price = 490000m,
                    ImageUrl = null,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProductVariant
                {
                    Id = Guid.Parse("fa000000-0000-0000-0000-000000000043"),
                    ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"),
                    SKU = "UQ-POLO-XL",
                    Price = 520000m,
                    ImageUrl = null,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

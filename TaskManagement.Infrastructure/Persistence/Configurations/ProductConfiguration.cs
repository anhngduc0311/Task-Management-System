using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.ProductCode)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasMaxLength(4000);

            builder.Property(p => p.DefaultPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(p => p.IsDeleted)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();

            builder.Property(p => p.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Relations
            builder.HasOne(p => p.BaseUnit)
                .WithMany()
                .HasForeignKey(p => p.BaseUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Origin)
                .WithMany()
                .HasForeignKey(p => p.OriginId)
                .OnDelete(DeleteBehavior.Restrict);

            // Soft delete query filter
            builder.HasQueryFilter(p => !p.IsDeleted);

            // Indexes
            builder.HasIndex(p => p.ProductCode)
                .HasDatabaseName("IX_Products_ProductCode")
                .HasFilter("[IsDeleted] = 0")
                .IsUnique();

            builder.HasIndex(p => p.CategoryId)
                .HasDatabaseName("IX_Products_CategoryId")
                .HasFilter("[IsDeleted] = 0 AND [CategoryId] IS NOT NULL");

            builder.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Products_Status")
                .HasFilter("[IsDeleted] = 0");

            builder.HasData(
                new Product
                {
                    Id = Guid.Parse("f0000000-0000-0000-0000-000000000001"),
                    ProductCode = "PROD_DELL_XPS13",
                    Name = "Laptop Dell XPS 13 9320",
                    Description = "Laptop cao cấp Dell XPS 13 Plus với chip Intel Core i7/i9 thế hệ 13, RAM 16GB/32GB, SSD 512GB/1TB.",
                    DefaultPrice = 45000000m,
                    BaseUnitId = Guid.Parse("f11e11a1-1111-1111-1111-111111111111"), // CAI
                    CategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000001"), // Electronics
                    Status = TaskManagement.Domain.Enums.ProductStatus.Active,
                    OriginId = Guid.Parse("d0000000-0000-0000-0000-000000000003"), // USA
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("f0000000-0000-0000-0000-000000000002"),
                    ProductCode = "PROD_VNM_MILK_180",
                    Name = "Sữa tươi Vinamilk ít đường 180ml",
                    Description = "Sữa tươi tiệt trùng Vinamilk ít đường, thơm ngon bổ dưỡng.",
                    DefaultPrice = 32000m,
                    BaseUnitId = Guid.Parse("f11e11a1-2222-2222-2222-222222222222"), // HOP
                    CategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000003"), // Food
                    Status = TaskManagement.Domain.Enums.ProductStatus.Active,
                    OriginId = Guid.Parse("d0000000-0000-0000-0000-000000000001"), // VN
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("f0000000-0000-0000-0000-000000000003"),
                    ProductCode = "PROD_TL_027",
                    Name = "Bút bi Thiên Long TL-027",
                    Description = "Bút bi mực xanh Thiên Long TL-027, viết trơn, đều mực, được ưa chuộng nhất.",
                    DefaultPrice = 4000m,
                    BaseUnitId = Guid.Parse("f11e11a1-1111-1111-1111-111111111111"), // CAI
                    CategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000004"), // Office
                    Status = TaskManagement.Domain.Enums.ProductStatus.Active,
                    OriginId = Guid.Parse("d0000000-0000-0000-0000-000000000001"), // VN
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("f0000000-0000-0000-0000-000000000004"),
                    ProductCode = "PROD_UNIQLO_POLO",
                    Name = "Áo thun Polo Nam Uniqlo",
                    Description = "Áo Polo nam Uniqlo chất liệu thun cotton thoáng mát, thấm hút mồ hôi tốt.",
                    DefaultPrice = 490000m,
                    BaseUnitId = Guid.Parse("f11e11a1-1111-1111-1111-111111111111"), // CAI
                    CategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000002"), // Fashion
                    Status = TaskManagement.Domain.Enums.ProductStatus.Active,
                    OriginId = Guid.Parse("d0000000-0000-0000-0000-000000000002"), // JP
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

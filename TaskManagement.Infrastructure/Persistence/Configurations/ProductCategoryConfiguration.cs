using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
    {
        public void Configure(EntityTypeBuilder<ProductCategory> builder)
        {
            builder.ToTable("ProductCategories");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            builder.Property(c => c.IsActive)
                .IsRequired();

            builder.Property(c => c.DisplayOrder)
                .IsRequired();

            builder.HasIndex(c => c.Code)
                .IsUnique();

            builder.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(
                new ProductCategory { Id = Guid.Parse("c0000000-0000-0000-0000-000000000001"), Code = "CAT_ELE", Name = "Điện tử", Description = "Thiết bị điện tử, công nghệ", IsActive = true, DisplayOrder = 1 },
                new ProductCategory { Id = Guid.Parse("c0000000-0000-0000-0000-000000000002"), Code = "CAT_FAS", Name = "Thời trang", Description = "Quần áo, phụ kiện thời trang", IsActive = true, DisplayOrder = 2 },
                new ProductCategory { Id = Guid.Parse("c0000000-0000-0000-0000-000000000003"), Code = "CAT_FNB", Name = "Thực phẩm & Đồ uống", Description = "Thức ăn, nước uống, sữa", IsActive = true, DisplayOrder = 3 },
                new ProductCategory { Id = Guid.Parse("c0000000-0000-0000-0000-000000000004"), Code = "CAT_OFF", Name = "Văn phòng phẩm", Description = "Bút, tập, dụng cụ văn phòng", IsActive = true, DisplayOrder = 4 }
            );
        }
    }
}

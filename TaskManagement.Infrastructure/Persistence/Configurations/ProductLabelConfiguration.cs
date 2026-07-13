using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductLabelConfiguration : IEntityTypeConfiguration<ProductLabel>
    {
        public void Configure(EntityTypeBuilder<ProductLabel> builder)
        {
            builder.ToTable("ProductLabels");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(l => l.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(l => l.Color)
                .HasMaxLength(50);

            builder.Property(l => l.IsActive)
                .IsRequired();

            builder.HasIndex(l => l.Code)
                 .IsUnique();

            builder.HasData(
                new ProductLabel { Id = Guid.Parse("e0000000-0000-0000-0000-000000000001"), Code = "LBL_HOT", Name = "Bán chạy", Color = "#ef4444", IsActive = true },
                new ProductLabel { Id = Guid.Parse("e0000000-0000-0000-0000-000000000002"), Code = "LBL_NEW", Name = "Sản phẩm mới", Color = "#10b981", IsActive = true },
                new ProductLabel { Id = Guid.Parse("e0000000-0000-0000-0000-000000000003"), Code = "LBL_SALE", Name = "Khuyến mãi", Color = "#f59e0b", IsActive = true },
                new ProductLabel { Id = Guid.Parse("e0000000-0000-0000-0000-000000000004"), Code = "LBL_PREMIUM", Name = "Cao cấp", Color = "#8b5cf6", IsActive = true }
            );
        }
    }
}

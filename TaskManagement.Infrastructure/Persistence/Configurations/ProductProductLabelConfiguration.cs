using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductProductLabelConfiguration : IEntityTypeConfiguration<ProductProductLabel>
    {
        public void Configure(EntityTypeBuilder<ProductProductLabel> builder)
        {
            builder.ToTable("ProductProductLabels");

            builder.HasKey(pl => new { pl.ProductId, pl.ProductLabelId });

            builder.HasOne(pl => pl.Product)
                .WithMany(p => p.ProductProductLabels)
                .HasForeignKey(pl => pl.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pl => pl.ProductLabel)
                .WithMany(l => l.ProductProductLabels)
                .HasForeignKey(pl => pl.ProductLabelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasData(
                new ProductProductLabel { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000001"), ProductLabelId = Guid.Parse("e0000000-0000-0000-0000-000000000004") }, // Dell XPS 13 -> Premium
                new ProductProductLabel { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000002"), ProductLabelId = Guid.Parse("e0000000-0000-0000-0000-000000000002") }, // Vinamilk -> New
                new ProductProductLabel { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000003"), ProductLabelId = Guid.Parse("e0000000-0000-0000-0000-000000000001") }, // Bút bi -> Bán chạy
                new ProductProductLabel { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"), ProductLabelId = Guid.Parse("e0000000-0000-0000-0000-000000000003") }  // Polo Uniqlo -> Khuyến mãi
            );
        }
    }
}

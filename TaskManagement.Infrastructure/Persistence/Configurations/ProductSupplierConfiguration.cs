using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProductSupplierConfiguration : IEntityTypeConfiguration<ProductSupplier>
    {
        public void Configure(EntityTypeBuilder<ProductSupplier> builder)
        {
            builder.ToTable("ProductSuppliers");

            builder.HasKey(ps => new { ps.ProductId, ps.SupplierId });

            builder.HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSuppliers)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ps => ps.Supplier)
                .WithMany(s => s.ProductSuppliers)
                .HasForeignKey(ps => ps.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasData(
                new ProductSupplier { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000001"), SupplierId = Guid.Parse("a0000000-0000-0000-0000-000000000001") }, // Dell XPS 13 -> Synnex FPT
                new ProductSupplier { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000001"), SupplierId = Guid.Parse("a0000000-0000-0000-0000-000000000002") }, // Dell XPS 13 -> Phong Vũ
                new ProductSupplier { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000002"), SupplierId = Guid.Parse("a0000000-0000-0000-0000-000000000003") }, // Vinamilk -> Vinamilk
                new ProductSupplier { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000003"), SupplierId = Guid.Parse("a0000000-0000-0000-0000-000000000004") }, // Bút bi -> Thiên Long
                new ProductSupplier { ProductId = Guid.Parse("f0000000-0000-0000-0000-000000000004"), SupplierId = Guid.Parse("a0000000-0000-0000-0000-000000000001") }  // Polo Uniqlo -> Synnex FPT
            );
        }
    }
}

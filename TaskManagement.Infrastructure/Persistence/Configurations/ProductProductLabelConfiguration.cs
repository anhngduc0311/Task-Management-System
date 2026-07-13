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
        }
    }
}

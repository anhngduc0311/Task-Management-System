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
        }
    }
}

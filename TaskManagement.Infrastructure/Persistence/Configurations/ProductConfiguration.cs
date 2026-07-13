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
        }
    }
}

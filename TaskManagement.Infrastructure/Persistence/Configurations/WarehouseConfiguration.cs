using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses");

            builder.HasKey(w => w.Id);

            builder.Property(w => w.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(w => w.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(w => w.Address)
                .HasMaxLength(500);

            builder.Property(w => w.Description)
                .HasMaxLength(500);

            builder.Property(w => w.IsActive)
                .IsRequired();

            builder.Property(w => w.CreatedAt)
                .IsRequired();

            builder.Property(w => w.UpdatedAt)
                .IsRequired();

            builder.HasIndex(w => w.Code)
                .IsUnique();

            // Seed default warehouse
            builder.HasData(
                new Warehouse
                {
                    Id = Guid.Parse("e11e11a1-1111-1111-1111-111111111111"),
                    Code = "WH01",
                    Name = "Main Warehouse",
                    Address = "123 Main Street",
                    Description = "Primary storage facility",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class UnitConfiguration : IEntityTypeConfiguration<Unit>
    {
        public void Configure(EntityTypeBuilder<Unit> builder)
        {
            builder.ToTable("Units");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(u => u.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(u => u.IsActive)
                .IsRequired();

            builder.HasIndex(u => u.Code)
                .IsUnique();

            // Seed data
            builder.HasData(
                new Unit { Id = Guid.Parse("f11e11a1-1111-1111-1111-111111111111"), Code = "CAI", Name = "Cái", IsActive = true },
                new Unit { Id = Guid.Parse("f11e11a1-2222-2222-2222-222222222222"), Code = "HOP", Name = "Hộp", IsActive = true },
                new Unit { Id = Guid.Parse("f11e11a1-3333-3333-3333-333333333333"), Code = "THUNG", Name = "Thùng", IsActive = true },
                new Unit { Id = Guid.Parse("f11e11a1-4444-4444-4444-444444444444"), Code = "KG", Name = "Kg", IsActive = true },
                new Unit { Id = Guid.Parse("f11e11a1-5555-5555-5555-555555555555"), Code = "G", Name = "G", IsActive = true }
            );
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class OriginConfiguration : IEntityTypeConfiguration<Origin>
    {
        public void Configure(EntityTypeBuilder<Origin> builder)
        {
            builder.ToTable("Origins");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(o => o.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(o => o.IsActive)
                .IsRequired();

            builder.HasIndex(o => o.Code)
                .IsUnique();

            builder.HasData(
                new Origin { Id = Guid.Parse("d0000000-0000-0000-0000-000000000001"), Code = "VN", Name = "Việt Nam", IsActive = true },
                new Origin { Id = Guid.Parse("d0000000-0000-0000-0000-000000000002"), Code = "JP", Name = "Nhật Bản", IsActive = true },
                new Origin { Id = Guid.Parse("d0000000-0000-0000-0000-000000000003"), Code = "US", Name = "Hoa Kỳ", IsActive = true },
                new Origin { Id = Guid.Parse("d0000000-0000-0000-0000-000000000004"), Code = "CN", Name = "Trung Quốc", IsActive = true }
            );
        }
    }
}

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
        }
    }
}

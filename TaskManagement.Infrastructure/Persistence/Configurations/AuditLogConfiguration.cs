using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(al => al.Id);

            builder.Property(al => al.Id)
                .ValueGeneratedOnAdd();

            builder.Property(al => al.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(al => al.EntityId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(al => al.Action)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(al => al.OldValue); // Implicitly NVARCHAR(MAX)

            builder.Property(al => al.NewValue); // Implicitly NVARCHAR(MAX)

            builder.Property(al => al.IpAddress)
                .HasMaxLength(50);

            builder.Property(al => al.UserAgent)
                .HasMaxLength(500);

            builder.Property(al => al.ChangedAt)
                .IsRequired();

            // Relations
            builder.HasOne(al => al.ChangedBy)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(al => new { al.EntityType, al.EntityId })
                .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

            builder.HasIndex(al => al.ChangedAt)
                .HasDatabaseName("IX_AuditLogs_ChangedAt");
        }
    }
}

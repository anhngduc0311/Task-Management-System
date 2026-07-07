using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
    {
        public void Configure(EntityTypeBuilder<TaskAttachment> builder)
        {
            builder.ToTable("TaskAttachments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.FileName)
                .HasMaxLength(260)
                .IsRequired();

            builder.Property(a => a.StorageKey)
                .HasMaxLength(512)
                .IsRequired();

            builder.Property(a => a.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.FileSize)
                .IsRequired();

            builder.Property(a => a.IsDeleted)
                .IsRequired();

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            // Soft delete global query filter
            builder.HasQueryFilter(a => !a.IsDeleted);

            // Relations
            builder.HasOne(a => a.Task)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.UploadedBy)
                .WithMany(u => u.Attachments)
                .HasForeignKey(a => a.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

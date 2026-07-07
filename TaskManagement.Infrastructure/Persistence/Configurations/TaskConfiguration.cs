using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;
using Task = TaskManagement.Domain.Entities.Task;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class TaskConfiguration : IEntityTypeConfiguration<Task>
    {
        public void Configure(EntityTypeBuilder<Task> builder)
        {
            builder.ToTable("Tasks");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(5000);

            builder.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.Priority)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.DueDate);

            builder.Property(t => t.IsDeleted)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.Property(t => t.UpdatedAt)
                .IsRequired();

            // Optimistic concurrency RowVersion config
            builder.Property(t => t.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Relations
            builder.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Assignee)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Soft delete global query filter
            builder.HasQueryFilter(t => !t.IsDeleted);

            // Filtered indexes
            builder.HasIndex(t => t.ProjectId)
                .HasDatabaseName("IX_Tasks_ProjectId")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(t => t.AssigneeId)
                .HasDatabaseName("IX_Tasks_AssigneeId")
                .HasFilter("[IsDeleted] = 0 AND [AssigneeId] IS NOT NULL");

            builder.HasIndex(t => new { t.ProjectId, t.Status })
                .HasDatabaseName("IX_Tasks_Status")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(t => t.DueDate)
                .HasDatabaseName("IX_Tasks_DueDate")
                .HasFilter("[IsDeleted] = 0 AND [DueDate] IS NOT NULL");
        }
    }
}

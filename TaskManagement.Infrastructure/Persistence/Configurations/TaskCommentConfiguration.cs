using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
    {
        public void Configure(EntityTypeBuilder<TaskComment> builder)
        {
            builder.ToTable("TaskComments");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Content)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(c => c.IsDeleted)
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt)
                .IsRequired();

            // Soft delete global query filter
            builder.HasQueryFilter(c => !c.IsDeleted);

            // Relations
            builder.HasOne(c => c.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.TaskId)
                .HasDatabaseName("IX_TaskComments_TaskId")
                .HasFilter("[IsDeleted] = 0");

            // Seed comments
            builder.HasData(
                new TaskComment
                {
                    Id = Guid.Parse("a01c01a0-1111-1111-1111-111111111111"),
                    TaskId = Guid.Parse("b22e22b2-2222-2222-2222-222222222222"),
                    UserId = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                    Content = "Hãy sử dụng các biểu đồ HSL và hiệu ứng hover mượt mà cho Dashboard nhé!",
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 5, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 5, 0, DateTimeKind.Utc)
                },
                new TaskComment
                {
                    Id = Guid.Parse("b02c02b0-2222-2222-2222-222222222222"),
                    TaskId = Guid.Parse("b22e22b2-2222-2222-2222-222222222222"),
                    UserId = Guid.Parse("2a98e29a-2454-4fbb-91bc-341aefba6222"),
                    Content = "Dạ, em đang phát triển giao diện theo thiết kế glassmorphic mượt mà.",
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 10, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 10, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

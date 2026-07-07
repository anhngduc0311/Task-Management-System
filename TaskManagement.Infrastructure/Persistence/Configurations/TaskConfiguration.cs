using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;
using Task = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using TaskPriority = TaskManagement.Domain.Enums.TaskPriority;

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

            builder.HasOne(t => t.ParentTask)
                .WithMany(t => t.ChildTasks)
                .HasForeignKey(t => t.ParentTaskId)
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

            // Seed tasks
            builder.HasData(
                new Task
                {
                    Id = Guid.Parse("a11e11a1-1111-1111-1111-111111111111"),
                    ProjectId = Guid.Parse("c7a52f44-8842-45e6-bd51-24ff43521234"),
                    Title = "Thiết kế cơ sở dữ liệu và bảo mật",
                    Description = "Thiết kế thực thể ERD, các cấu hình bảo mật CORS, Headers, và Phân quyền API.",
                    Status = TaskStatus.Done,
                    Priority = TaskPriority.Critical,
                    AssigneeId = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                    CreatedById = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                    DueDate = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Task
                {
                    Id = Guid.Parse("b22e22b2-2222-2222-2222-222222222222"),
                    ProjectId = Guid.Parse("c7a52f44-8842-45e6-bd51-24ff43521234"),
                    Title = "Xây dựng màn hình Dashboard trực quan",
                    Description = "Phát triển giao diện Angular hiển thị biểu đồ danh sách công việc, phân tích trạng thái.",
                    Status = TaskStatus.InProgress,
                    Priority = TaskPriority.High,
                    AssigneeId = Guid.Parse("2a98e29a-2454-4fbb-91bc-341aefba6222"),
                    CreatedById = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                    DueDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Task
                {
                    Id = Guid.Parse("c33e33c3-3333-3333-3333-333333333333"),
                    ProjectId = Guid.Parse("c7a52f44-8842-45e6-bd51-24ff43521234"),
                    Title = "Viết tài liệu API & tích hợp Swagger",
                    Description = "Cập nhật Swagger OpenAPI để tự động hóa tài liệu cho các endpoint.",
                    Status = TaskStatus.Todo,
                    Priority = TaskPriority.Medium,
                    AssigneeId = Guid.Parse("3f78e7aa-2e45-424a-81a1-f3b17789a333"),
                    CreatedById = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                    DueDate = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

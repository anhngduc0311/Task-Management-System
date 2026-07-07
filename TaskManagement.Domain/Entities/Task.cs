using System;
using System.Collections.Generic;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Entities
{
    public class Task
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Todo;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public Guid? AssigneeId { get; set; }
        public User? Assignee { get; set; }

        public Guid CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Concurrency token
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public Guid? ParentTaskId { get; set; }
        public Task? ParentTask { get; set; }
        public ICollection<Task> ChildTasks { get; set; } = new List<Task>();

        // Navigation Properties
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        public ICollection<TaskDynamicFieldValue> DynamicFieldValues { get; set; } = new List<TaskDynamicFieldValue>();

        // Business Rules
        public void UpdateTitle(string newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle))
                throw new ArgumentException("Task title cannot be empty.");

            if (newTitle.Length > 200)
                throw new ArgumentException("Task title cannot exceed 200 characters.");

            Title = newTitle;
            UpdatedAt = DateTime.UtcNow;
        }

        public void TransitionTo(TaskStatus newStatus)
        {
            // Once Cancelled, the task can only be reopened to Todo
            if (Status == TaskStatus.Cancelled && newStatus != TaskStatus.Todo)
            {
                throw new InvalidOperationException("A cancelled task can only be reopened to Todo status.");
            }

            // Cannot transition to the same status
            if (Status == newStatus) return;

            if (newStatus == TaskStatus.Done)
            {
                CompletedAt = DateTime.UtcNow;
            }
            else
            {
                CompletedAt = null;
            }

            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

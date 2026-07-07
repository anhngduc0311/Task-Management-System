using System;

namespace TaskManagement.Application.DTOs.Tasks
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public Guid? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public Guid CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public Guid? ParentTaskId { get; set; }
        public string? ParentTaskTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string RowVersion { get; set; } = string.Empty; // Base64 string
        public int SubtasksCount { get; set; }
        public int CompletedSubtasksCount { get; set; }
        public System.Collections.Generic.List<SubTaskDto> ChildTasks { get; set; } = new System.Collections.Generic.List<SubTaskDto>();
    }

    public class SubTaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

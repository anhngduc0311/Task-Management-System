using System;

namespace TaskManagement.Domain.Entities
{
    public class TaskComment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TaskId { get; set; }
        public Task Task { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Business Rules
        public void UpdateContent(string newContent)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Comment content cannot be empty.");

            if (newContent.Length > 2000)
                throw new ArgumentException("Comment content cannot exceed 2000 characters.");

            Content = newContent;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

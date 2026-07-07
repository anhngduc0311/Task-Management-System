using System;

namespace TaskManagement.Domain.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public string EntityType { get; set; } = string.Empty; // e.g., Task, Project
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // e.g., Created, Updated, StatusChanged

        public Guid ChangedById { get; set; }
        public User ChangedBy { get; set; } = null!;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? OldValue { get; set; } // JSON serialized
        public string? NewValue { get; set; } // JSON serialized

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}

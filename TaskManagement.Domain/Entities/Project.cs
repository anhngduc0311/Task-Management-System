using System;
using System.Collections.Generic;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}

using System;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class ProjectMember
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public ProjectMemberRole RoleInProject { get; set; } = ProjectMemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public ProjectMemberStatus Status { get; set; } = ProjectMemberStatus.Active;
    }
}

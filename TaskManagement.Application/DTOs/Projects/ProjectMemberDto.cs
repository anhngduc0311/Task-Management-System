using System;

namespace TaskManagement.Application.DTOs.Projects
{
    public class ProjectMemberDto
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleInProject { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

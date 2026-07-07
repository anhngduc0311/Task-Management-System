using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Projects
{
    public class AddProjectMemberDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [RegularExpression("^(ProjectManager|Member|Guest)$", ErrorMessage = "RoleInProject must be 'ProjectManager', 'Member', or 'Guest'.")]
        public string RoleInProject { get; set; } = "Member";
    }
}

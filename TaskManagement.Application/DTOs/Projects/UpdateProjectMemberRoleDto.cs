using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Projects
{
    public class UpdateProjectMemberRoleDto
    {
        [Required]
        [RegularExpression("^(ProjectManager|Member|Guest)$", ErrorMessage = "RoleInProject must be 'ProjectManager', 'Member', or 'Guest'.")]
        public string RoleInProject { get; set; } = string.Empty;
    }
}

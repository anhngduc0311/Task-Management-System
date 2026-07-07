using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Projects
{
    public class UpdateProjectDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [RegularExpression("^(Active|Archived|Deleted)$", ErrorMessage = "Status must be 'Active', 'Archived', or 'Deleted'.")]
        public string Status { get; set; } = string.Empty;
    }
}

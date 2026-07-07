using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Users
{
    public class UpdateUserStatusDto
    {
        [Required]
        [RegularExpression("^(Active|Inactive)$", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
        public string Status { get; set; } = string.Empty;
    }
}

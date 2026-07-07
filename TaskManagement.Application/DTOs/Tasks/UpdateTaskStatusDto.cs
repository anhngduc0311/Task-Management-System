using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Tasks
{
    public class UpdateTaskStatusDto
    {
        [Required]
        [RegularExpression("^(Todo|InProgress|InReview|Done|Cancelled)$", ErrorMessage = "Status must be 'Todo', 'InProgress', 'InReview', 'Done', or 'Cancelled'.")]
        public string Status { get; set; } = string.Empty;
    }
}

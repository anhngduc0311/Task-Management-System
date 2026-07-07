using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Tasks
{
    public class CreateTaskDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(5000)]
        public string? Description { get; set; }

        [Required]
        [RegularExpression("^(Low|Medium|High|Critical)$", ErrorMessage = "Priority must be 'Low', 'Medium', 'High', or 'Critical'.")]
        public string Priority { get; set; } = "Medium";

        public Guid? AssigneeId { get; set; }

        public DateTime? DueDate { get; set; }

        public Guid? ParentTaskId { get; set; }
    }
}

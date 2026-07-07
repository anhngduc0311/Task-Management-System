using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Tasks
{
    public class UpdateTaskDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(5000)]
        public string? Description { get; set; }

        [Required]
        [RegularExpression("^(Todo|InProgress|InReview|Done|Cancelled)$", ErrorMessage = "Status must be 'Todo', 'InProgress', 'InReview', 'Done', or 'Cancelled'.")]
        public string Status { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Low|Medium|High|Critical)$", ErrorMessage = "Priority must be 'Low', 'Medium', 'High', or 'Critical'.")]
        public string Priority { get; set; } = string.Empty;

        public Guid? AssigneeId { get; set; }

        public DateTime? DueDate { get; set; }

        public Guid? ParentTaskId { get; set; }
        
        [Required]
        public string RowVersion { get; set; } = string.Empty; // Base64 string

        public System.Collections.Generic.Dictionary<string, string>? DynamicValues { get; set; }
    }
}

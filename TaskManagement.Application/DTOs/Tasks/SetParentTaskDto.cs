using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Tasks
{
    public class SetParentTaskDto
    {
        [Required]
        public Guid ParentTaskId { get; set; }
    }
}

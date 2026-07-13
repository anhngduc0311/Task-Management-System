using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.ProductCategories
{
    public class CreateCategoryDto
    {
        public Guid? ParentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;
    }
}

using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.ProductLabels
{
    public class UpdateLabelDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Color { get; set; }

        public bool IsActive { get; set; }
    }
}

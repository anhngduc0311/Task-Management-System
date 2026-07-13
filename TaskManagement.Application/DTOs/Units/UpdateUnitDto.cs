using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Units
{
    public class UpdateUnitDto
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}

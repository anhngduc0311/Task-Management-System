using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Warehouses
{
    public class UpdateWarehouseDto
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Suppliers
{
    public class UpdateSupplierDto
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        [MaxLength(150)]
        public string? ContactPerson { get; set; }

        public bool IsActive { get; set; }
    }
}

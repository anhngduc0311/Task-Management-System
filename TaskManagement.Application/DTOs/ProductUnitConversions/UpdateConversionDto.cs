using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.ProductUnitConversions
{
    public class UpdateConversionDto
    {
        [Required]
        public Guid FromUnitId { get; set; }

        [Required]
        public Guid ToUnitId { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Conversion rate must be greater than zero.")]
        public decimal ConversionRate { get; set; }
    }
}

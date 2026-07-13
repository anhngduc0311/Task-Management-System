using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Products
{
    public class UpdateProductDto
    {
        [Required]
        [MaxLength(100)]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Default price cannot be negative.")]
        public decimal DefaultPrice { get; set; }

        [Required]
        public Guid BaseUnitId { get; set; }

        public Guid? CategoryId { get; set; }

        [Required]
        [RegularExpression("^(Active|Inactive|Discontinued)$", ErrorMessage = "Status must be 'Active', 'Inactive', or 'Discontinued'.")]
        public string Status { get; set; } = "Active";

        public Guid? OriginId { get; set; }

        public List<Guid>? SupplierIds { get; set; }
        public List<Guid>? LabelIds { get; set; }

        [Required]
        public string RowVersion { get; set; } = string.Empty;
    }
}

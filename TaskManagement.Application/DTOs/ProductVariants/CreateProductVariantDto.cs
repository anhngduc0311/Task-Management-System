using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.ProductVariants
{
    public class CreateProductVariantDto
    {
        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
        public decimal? Price { get; set; }

        [MaxLength(2000)]
        public string? ImageUrl { get; set; }

        [Required]
        public List<Guid> AttributeValueIds { get; set; } = new List<Guid>();
    }
}

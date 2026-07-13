using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.ProductVariants
{
    public class UpdateProductVariantDto
    {
        [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
        public decimal? Price { get; set; }

        [MaxLength(2000)]
        public string? ImageUrl { get; set; }

        [Required]
        public string RowVersion { get; set; } = string.Empty;
    }
}

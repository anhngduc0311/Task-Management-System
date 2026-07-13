using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Products
{
    public class CreateProductAttributeValueDto
    {
        [Required]
        [MaxLength(100)]
        public string Value { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
    }
}

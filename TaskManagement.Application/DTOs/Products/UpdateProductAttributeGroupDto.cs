using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Products
{
    public class UpdateProductAttributeGroupDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
    }
}

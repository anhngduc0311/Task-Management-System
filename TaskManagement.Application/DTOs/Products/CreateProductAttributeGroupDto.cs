using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Products
{
    public class CreateProductAttributeGroupDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        [Required]
        public List<string> Values { get; set; } = new List<string>();
    }
}

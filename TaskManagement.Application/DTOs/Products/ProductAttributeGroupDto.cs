using System;
using System.Collections.Generic;

namespace TaskManagement.Application.DTOs.Products
{
    public class ProductAttributeGroupDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<ProductAttributeValueDto> Values { get; set; } = new List<ProductAttributeValueDto>();
    }
}

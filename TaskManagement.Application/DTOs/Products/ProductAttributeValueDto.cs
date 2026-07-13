using System;

namespace TaskManagement.Application.DTOs.Products
{
    public class ProductAttributeValueDto
    {
        public Guid Id { get; set; }
        public Guid AttributeGroupId { get; set; }
        public string Value { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}

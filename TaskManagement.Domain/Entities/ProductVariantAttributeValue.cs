using System;

namespace TaskManagement.Domain.Entities
{
    public class ProductVariantAttributeValue
    {
        public Guid ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        public Guid ProductAttributeValueId { get; set; }
        public ProductAttributeValue ProductAttributeValue { get; set; } = null!;
    }
}

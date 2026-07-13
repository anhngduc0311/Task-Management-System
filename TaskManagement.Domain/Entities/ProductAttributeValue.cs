using System;
using System.Collections.Generic;

namespace TaskManagement.Domain.Entities
{
    public class ProductAttributeValue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AttributeGroupId { get; set; }
        public ProductAttributeGroup AttributeGroup { get; set; } = null!;

        public string Value { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        public ICollection<ProductVariantAttributeValue> VariantAttributeValues { get; set; } = new List<ProductVariantAttributeValue>();
    }
}

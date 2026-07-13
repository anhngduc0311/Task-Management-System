using System;
using System.Collections.Generic;

namespace TaskManagement.Domain.Entities
{
    public class ProductAttributeGroup
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;

        public ICollection<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();
    }
}

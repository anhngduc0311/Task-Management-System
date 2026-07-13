using System;
using System.Collections.Generic;

namespace TaskManagement.Domain.Entities
{
    public class ProductVariant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string SKU { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<ProductVariantAttributeValue> VariantAttributeValues { get; set; } = new List<ProductVariantAttributeValue>();
    }
}

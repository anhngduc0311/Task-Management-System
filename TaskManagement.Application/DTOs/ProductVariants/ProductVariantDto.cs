using System;
using System.Collections.Generic;

namespace TaskManagement.Application.DTOs.ProductVariants
{
    public class ProductVariantDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
        public List<Guid> AttributeValueIds { get; set; } = new List<Guid>();
        public string AttributeValueCombinations { get; set; } = string.Empty;
        public string RowVersion { get; set; } = string.Empty;
    }
}

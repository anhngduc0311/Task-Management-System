using System;
using System.Collections.Generic;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DefaultPrice { get; set; } = 0;

        public Guid BaseUnitId { get; set; }
        public Unit BaseUnit { get; set; } = null!;

        public Guid? CategoryId { get; set; }
        public ProductCategory? Category { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public Guid? OriginId { get; set; }
        public Origin? Origin { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation Properties
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductUnitConversion> UnitConversions { get; set; } = new List<ProductUnitConversion>();
        public ICollection<ProductSupplier> ProductSuppliers { get; set; } = new List<ProductSupplier>();
        public ICollection<ProductAttributeGroup> AttributeGroups { get; set; } = new List<ProductAttributeGroup>();
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductProductLabel> ProductProductLabels { get; set; } = new List<ProductProductLabel>();

        public void AddAttributeGroup(ProductAttributeGroup group)
        {
            if (AttributeGroups.Count >= 2)
            {
                throw new InvalidOperationException("A product can have a maximum of 2 attribute groups.");
            }
            AttributeGroups.Add(group);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

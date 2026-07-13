using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskManagement.Domain.Entities
{
    public class ProductCategory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentId { get; set; }
        public ProductCategory? Parent { get; set; }
        public ICollection<ProductCategory> Children { get; set; } = new List<ProductCategory>();

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        public ICollection<Product> Products { get; set; } = new List<Product>();

        public void UpdateParent(Guid? parentId, IEnumerable<ProductCategory> allCategories)
        {
            if (parentId == null)
            {
                ParentId = null;
                Parent = null;
                return;
            }

            if (parentId == Id)
                throw new InvalidOperationException("A category cannot be its own parent.");

            // Check circular reference by traversing up using the in-memory collection of categories
            var categoryMap = allCategories.ToDictionary(c => c.Id);
            var currentParentId = parentId;
            while (currentParentId != null)
            {
                if (currentParentId == Id)
                    throw new InvalidOperationException("Circular reference detected in category hierarchy.");

                if (categoryMap.TryGetValue(currentParentId.Value, out var parentCategory))
                {
                    currentParentId = parentCategory.ParentId;
                }
                else
                {
                    break;
                }
            }

            ParentId = parentId;
        }
    }
}

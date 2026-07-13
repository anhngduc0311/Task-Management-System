using System;
using System.Collections.Generic;

namespace TaskManagement.Domain.Entities
{
    public class ProductLabel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Color { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<ProductProductLabel> ProductProductLabels { get; set; } = new List<ProductProductLabel>();
    }
}

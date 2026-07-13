using System;

namespace TaskManagement.Domain.Entities
{
    public class ProductProductLabel
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid ProductLabelId { get; set; }
        public ProductLabel ProductLabel { get; set; } = null!;
    }
}

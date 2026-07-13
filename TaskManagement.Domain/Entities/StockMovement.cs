using System;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class StockMovement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public decimal Quantity { get; set; }
        public MovementType MovementType { get; set; }

        public Guid? ReferenceId { get; set; }
        public string? ReferenceNo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

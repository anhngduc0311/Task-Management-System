using System;

namespace TaskManagement.Domain.Entities
{
    public class ExportReceiptLine
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ExportReceiptId { get; set; }
        public ExportReceipt ExportReceipt { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public decimal Quantity { get; set; }
        public Guid UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        public decimal BaseQuantity { get; set; }
        public decimal ConversionRate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

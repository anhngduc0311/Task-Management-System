using System;

namespace TaskManagement.Domain.Entities
{
    public class ImportReceiptLine
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ImportReceiptId { get; set; }
        public ImportReceipt ImportReceipt { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public decimal Quantity { get; set; }
        public Guid UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal ConversionRate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

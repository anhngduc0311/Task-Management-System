using System;

namespace TaskManagement.Domain.Entities
{
    public class TransferReceiptLine
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TransferReceiptId { get; set; }
        public TransferReceipt TransferReceipt { get; set; } = null!;

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

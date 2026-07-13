using System;
using System.Collections.Generic;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class ImportReceipt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ReceiptNo { get; set; } = string.Empty;
        public Guid? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public Guid WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public ReceiptStatus Status { get; set; } = ReceiptStatus.Draft;
        public string? Description { get; set; }
        public decimal TotalAmount { get; set; } = 0;

        public Guid CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<ImportReceiptLine> Lines { get; set; } = new List<ImportReceiptLine>();
    }
}

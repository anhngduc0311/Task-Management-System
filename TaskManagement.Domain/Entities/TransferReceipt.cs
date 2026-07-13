using System;
using System.Collections.Generic;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class TransferReceipt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ReceiptNo { get; set; } = string.Empty;

        public Guid FromWarehouseId { get; set; }
        public Warehouse FromWarehouse { get; set; } = null!;

        public Guid ToWarehouseId { get; set; }
        public Warehouse ToWarehouse { get; set; } = null!;

        public ReceiptStatus Status { get; set; } = ReceiptStatus.Draft;
        public string? Description { get; set; }

        public Guid CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<TransferReceiptLine> Lines { get; set; } = new List<TransferReceiptLine>();

        public void ValidateReceipt()
        {
            if (FromWarehouseId == ToWarehouseId)
            {
                throw new InvalidOperationException("Source and destination warehouses cannot be the same.");
            }
        }
    }
}

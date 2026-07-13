using System;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Stock
{
    public class StockMovementDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;

        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;

        public Guid? ProductVariantId { get; set; }
        public string? VariantSKU { get; set; }

        public decimal Quantity { get; set; }
        public MovementType MovementType { get; set; }

        public Guid? ReferenceId { get; set; }
        public string? ReferenceNo { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

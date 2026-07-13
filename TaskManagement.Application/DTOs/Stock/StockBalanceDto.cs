using System;

namespace TaskManagement.Application.DTOs.Stock
{
    public class StockBalanceDto
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
        public DateTime LastUpdatedAt { get; set; }
    }
}

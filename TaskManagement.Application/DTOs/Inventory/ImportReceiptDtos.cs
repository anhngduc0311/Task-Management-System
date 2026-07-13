using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Inventory
{
    public class ImportReceiptDto
    {
        public Guid Id { get; set; }
        public string ReceiptNo { get; set; } = string.Empty;
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public ReceiptStatus Status { get; set; }
        public string? Description { get; set; }
        public decimal TotalAmount { get; set; }
        public Guid CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<ImportReceiptLineDto> Lines { get; set; } = new List<ImportReceiptLineDto>();
    }

    public class ImportReceiptLineDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public Guid? ProductVariantId { get; set; }
        public string? VariantSKU { get; set; }
        public decimal Quantity { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class CreateImportReceiptDto
    {
        public Guid? SupplierId { get; set; }

        [Required]
        public Guid WarehouseId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Receipt must contain at least one line item.")]
        public List<CreateImportReceiptLineDto> Lines { get; set; } = new List<CreateImportReceiptLineDto>();
    }

    public class CreateImportReceiptLineDto
    {
        [Required]
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }

        [Required]
        public Guid UnitId { get; set; }

        [Range(0.0, double.MaxValue, ErrorMessage = "UnitPrice cannot be negative.")]
        public decimal UnitPrice { get; set; }
    }

    public class UpdateImportReceiptDto
    {
        public Guid? SupplierId { get; set; }

        [Required]
        public Guid WarehouseId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Receipt must contain at least one line item.")]
        public List<CreateImportReceiptLineDto> Lines { get; set; } = new List<CreateImportReceiptLineDto>();

        [Required]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

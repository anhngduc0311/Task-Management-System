using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Inventory;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/inventory/import-receipts")]
    public class ImportReceiptsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IStockService _stockService;
        private readonly IUnitConversionService _unitConversionService;
        private readonly IAuditService _auditService;

        public ImportReceiptsController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IStockService stockService,
            IUnitConversionService unitConversionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _stockService = stockService;
            _unitConversionService = unitConversionService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? warehouseId = null,
            [FromQuery] Guid? supplierId = null,
            [FromQuery] ReceiptStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var query = _dbContext.ImportReceipts
                .Include(r => r.Warehouse)
                .Include(r => r.Supplier)
                .Include(r => r.CreatedBy)
                .AsQueryable();

            if (warehouseId.HasValue)
            {
                query = query.Where(r => r.WarehouseId == warehouseId.Value);
            }
            if (supplierId.HasValue)
            {
                query = query.Where(r => r.SupplierId == supplierId.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var total = await query.CountAsync();
            var skip = (page - 1) * pageSize;
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(r => new ImportReceiptDto
                {
                    Id = r.Id,
                    ReceiptNo = r.ReceiptNo,
                    SupplierId = r.SupplierId,
                    SupplierName = r.Supplier != null ? r.Supplier.Name : null,
                    WarehouseId = r.WarehouseId,
                    WarehouseName = r.Warehouse.Name,
                    WarehouseCode = r.Warehouse.Code,
                    Status = r.Status,
                    Description = r.Description,
                    TotalAmount = r.TotalAmount,
                    CreatedById = r.CreatedById,
                    CreatedByName = r.CreatedBy.FullName,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    RowVersion = r.RowVersion
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var r = await _dbContext.ImportReceipts
                .Include(x => x.Warehouse)
                .Include(x => x.Supplier)
                .Include(x => x.CreatedBy)
                .Include(x => x.Lines)
                    .ThenInclude(l => l.Product)
                .Include(x => x.Lines)
                    .ThenInclude(l => l.ProductVariant)
                .Include(x => x.Lines)
                    .ThenInclude(l => l.Unit)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null)
            {
                return NotFound(new { message = "Import receipt not found." });
            }

            var dto = new ImportReceiptDto
            {
                Id = r.Id,
                ReceiptNo = r.ReceiptNo,
                SupplierId = r.SupplierId,
                SupplierName = r.Supplier != null ? r.Supplier.Name : null,
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse.Name,
                WarehouseCode = r.Warehouse.Code,
                Status = r.Status,
                Description = r.Description,
                TotalAmount = r.TotalAmount,
                CreatedById = r.CreatedById,
                CreatedByName = r.CreatedBy.FullName,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                RowVersion = r.RowVersion,
                Lines = r.Lines.Select(line => new ImportReceiptLineDto
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.Product.Name,
                    ProductCode = line.Product.ProductCode,
                    ProductVariantId = line.ProductVariantId,
                    VariantSKU = line.ProductVariant != null ? line.ProductVariant.SKU : null,
                    Quantity = line.Quantity,
                    UnitId = line.UnitId,
                    UnitName = line.Unit.Name,
                    UnitPrice = line.UnitPrice,
                    Amount = line.Amount,
                    BaseQuantity = line.BaseQuantity,
                    ConversionRate = line.ConversionRate
                }).ToList()
            };

            return Ok(dto);
        }

        private async Task<string> GenerateReceiptNoAsync(string prefix)
        {
            var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var searchPattern = $"{prefix}-{todayStr}-";

            var maxReceiptNo = await _dbContext.ImportReceipts
                .Where(r => r.ReceiptNo.StartsWith(searchPattern))
                .OrderByDescending(r => r.ReceiptNo)
                .Select(r => r.ReceiptNo)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (!string.IsNullOrEmpty(maxReceiptNo))
            {
                var parts = maxReceiptNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                {
                    nextNum = lastNum + 1;
                }
            }

            return $"{prefix}-{todayStr}-{nextNum:D3}";
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateImportReceiptDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageWarehouseReceiptsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var warehouse = await _dbContext.Warehouses.FindAsync(dto.WarehouseId);
            if (warehouse == null || !warehouse.IsActive)
            {
                return BadRequest(new { message = "Warehouse not found or is inactive." });
            }

            if (dto.SupplierId.HasValue)
            {
                var supplier = await _dbContext.Suppliers.FindAsync(dto.SupplierId.Value);
                if (supplier == null || !supplier.IsActive)
                {
                    return BadRequest(new { message = "Supplier not found or is inactive." });
                }
            }

            var receiptNo = await GenerateReceiptNoAsync("NK");

            var receipt = new ImportReceipt
            {
                ReceiptNo = receiptNo,
                SupplierId = dto.SupplierId,
                WarehouseId = dto.WarehouseId,
                Status = ReceiptStatus.Draft,
                Description = dto.Description,
                CreatedById = CurrentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            decimal totalAmount = 0;

            foreach (var lineDto in dto.Lines)
            {
                var product = await _dbContext.Products.FindAsync(lineDto.ProductId);
                if (product == null || product.IsDeleted)
                {
                    return BadRequest(new { message = $"Product {lineDto.ProductId} not found." });
                }

                if (product.Status != ProductStatus.Active)
                {
                    return BadRequest(new { message = $"Product {product.Name} is not active." });
                }

                if (lineDto.ProductVariantId.HasValue)
                {
                    var variant = await _dbContext.ProductVariants
                        .FirstOrDefaultAsync(v => v.Id == lineDto.ProductVariantId.Value && v.ProductId == lineDto.ProductId);
                    if (variant == null)
                    {
                        return BadRequest(new { message = $"Variant {lineDto.ProductVariantId} does not belong to product {product.Name}." });
                    }
                }

                decimal baseQty;
                try
                {
                    baseQty = await _unitConversionService.ConvertToBaseUnitAsync(lineDto.ProductId, lineDto.UnitId, lineDto.Quantity);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }

                decimal convRate = baseQty / lineDto.Quantity;
                var amount = lineDto.Quantity * lineDto.UnitPrice;
                totalAmount += amount;

                receipt.Lines.Add(new ImportReceiptLine
                {
                    ProductId = lineDto.ProductId,
                    ProductVariantId = lineDto.ProductVariantId,
                    Quantity = lineDto.Quantity,
                    UnitId = lineDto.UnitId,
                    UnitPrice = lineDto.UnitPrice,
                    Amount = amount,
                    BaseQuantity = baseQty,
                    ConversionRate = convRate,
                    CreatedAt = DateTime.UtcNow
                });
            }

            receipt.TotalAmount = totalAmount;
            _dbContext.ImportReceipts.Add(receipt);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "ImportReceipt",
                receipt.Id.ToString(),
                "Created",
                CurrentUserId,
                null,
                JsonSerializer.Serialize(new { receipt.ReceiptNo, receipt.WarehouseId, receipt.SupplierId, receipt.TotalAmount }),
                ClientIpAddress,
                ClientUserAgent
            );

            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, new { id = receipt.Id, receiptNo = receipt.ReceiptNo });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateImportReceiptDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageWarehouseReceiptsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var receipt = await _dbContext.ImportReceipts
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
            {
                return NotFound(new { message = "Import receipt not found." });
            }

            if (receipt.Status != ReceiptStatus.Draft)
            {
                return BadRequest(new { message = "Only draft receipts can be modified." });
            }

            if (!receipt.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Conflict(new { message = "The record has been modified by another user." });
            }

            var warehouse = await _dbContext.Warehouses.FindAsync(dto.WarehouseId);
            if (warehouse == null || !warehouse.IsActive)
            {
                return BadRequest(new { message = "Warehouse not found or is inactive." });
            }

            if (dto.SupplierId.HasValue)
            {
                var supplier = await _dbContext.Suppliers.FindAsync(dto.SupplierId.Value);
                if (supplier == null || !supplier.IsActive)
                {
                    return BadRequest(new { message = "Supplier not found or is inactive." });
                }
            }

            var oldValue = JsonSerializer.Serialize(new
            {
                receipt.WarehouseId,
                receipt.SupplierId,
                receipt.Description,
                receipt.TotalAmount,
                LinesCount = receipt.Lines.Count
            });

            // Clear old lines
            _dbContext.ImportReceiptLines.RemoveRange(receipt.Lines);
            receipt.Lines.Clear();

            decimal totalAmount = 0;

            foreach (var lineDto in dto.Lines)
            {
                var product = await _dbContext.Products.FindAsync(lineDto.ProductId);
                if (product == null || product.IsDeleted)
                {
                    return BadRequest(new { message = $"Product {lineDto.ProductId} not found." });
                }
                if (product.Status != ProductStatus.Active)
                {
                    return BadRequest(new { message = $"Product {product.Name} is not active." });
                }

                if (lineDto.ProductVariantId.HasValue)
                {
                    var variant = await _dbContext.ProductVariants
                        .FirstOrDefaultAsync(v => v.Id == lineDto.ProductVariantId.Value && v.ProductId == lineDto.ProductId);
                    if (variant == null)
                    {
                        return BadRequest(new { message = $"Variant {lineDto.ProductVariantId} does not belong to product {product.Name}." });
                    }
                }

                decimal baseQty;
                try
                {
                    baseQty = await _unitConversionService.ConvertToBaseUnitAsync(lineDto.ProductId, lineDto.UnitId, lineDto.Quantity);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }

                decimal convRate = baseQty / lineDto.Quantity;
                var amount = lineDto.Quantity * lineDto.UnitPrice;
                totalAmount += amount;

                receipt.Lines.Add(new ImportReceiptLine
                {
                    ProductId = lineDto.ProductId,
                    ProductVariantId = lineDto.ProductVariantId,
                    Quantity = lineDto.Quantity,
                    UnitId = lineDto.UnitId,
                    UnitPrice = lineDto.UnitPrice,
                    Amount = amount,
                    BaseQuantity = baseQty,
                    ConversionRate = convRate,
                    CreatedAt = DateTime.UtcNow
                });
            }

            receipt.SupplierId = dto.SupplierId;
            receipt.WarehouseId = dto.WarehouseId;
            receipt.Description = dto.Description;
            receipt.TotalAmount = totalAmount;
            receipt.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "ImportReceipt",
                receipt.Id.ToString(),
                "Updated",
                CurrentUserId,
                oldValue,
                JsonSerializer.Serialize(new { receipt.WarehouseId, receipt.SupplierId, receipt.TotalAmount }),
                ClientIpAddress,
                ClientUserAgent
            );

            return NoContent();
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> Confirm(Guid id)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            try
            {
                await _stockService.ConfirmImportReceiptAsync(id, CurrentUserId);

                await _auditService.LogAsync(
                    "ImportReceipt",
                    id.ToString(),
                    "Confirmed",
                    CurrentUserId,
                    JsonSerializer.Serialize(new { Status = ReceiptStatus.Draft.ToString() }),
                    JsonSerializer.Serialize(new { Status = ReceiptStatus.Confirmed.ToString() }),
                    ClientIpAddress,
                    ClientUserAgent
                );

                return Ok(new { message = "Receipt confirmed successfully." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            try
            {
                await _stockService.CancelImportReceiptAsync(id, CurrentUserId);

                await _auditService.LogAsync(
                    "ImportReceipt",
                    id.ToString(),
                    "Cancelled",
                    CurrentUserId,
                    JsonSerializer.Serialize(new { Status = ReceiptStatus.Confirmed.ToString() }),
                    JsonSerializer.Serialize(new { Status = ReceiptStatus.Cancelled.ToString() }),
                    ClientIpAddress,
                    ClientUserAgent
                );

                return Ok(new { message = "Receipt cancelled successfully." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

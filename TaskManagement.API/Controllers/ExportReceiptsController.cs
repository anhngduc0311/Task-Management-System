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
    [Route("api/inventory/export-receipts")]
    public class ExportReceiptsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IStockService _stockService;
        private readonly IUnitConversionService _unitConversionService;
        private readonly IAuditService _auditService;

        public ExportReceiptsController(
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
            [FromQuery] ReceiptStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var query = _dbContext.ExportReceipts
                .Include(r => r.Warehouse)
                .Include(r => r.CreatedBy)
                .AsQueryable();

            if (warehouseId.HasValue)
            {
                query = query.Where(r => r.WarehouseId == warehouseId.Value);
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
                .Select(r => new ExportReceiptDto
                {
                    Id = r.Id,
                    ReceiptNo = r.ReceiptNo,
                    WarehouseId = r.WarehouseId,
                    WarehouseName = r.Warehouse.Name,
                    WarehouseCode = r.Warehouse.Code,
                    Status = r.Status,
                    Description = r.Description,
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

            var r = await _dbContext.ExportReceipts
                .Include(x => x.Warehouse)
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
                return NotFound(new { message = "Export receipt not found." });
            }

            var dto = new ExportReceiptDto
            {
                Id = r.Id,
                ReceiptNo = r.ReceiptNo,
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse.Name,
                WarehouseCode = r.Warehouse.Code,
                Status = r.Status,
                Description = r.Description,
                CreatedById = r.CreatedById,
                CreatedByName = r.CreatedBy.FullName,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                RowVersion = r.RowVersion,
                Lines = r.Lines.Select(line => new ExportReceiptLineDto
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

            var maxReceiptNo = await _dbContext.ExportReceipts
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
        public async Task<IActionResult> Create([FromBody] CreateExportReceiptDto dto)
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

            var receiptNo = await GenerateReceiptNoAsync("XK");

            var receipt = new ExportReceipt
            {
                ReceiptNo = receiptNo,
                WarehouseId = dto.WarehouseId,
                Status = ReceiptStatus.Draft,
                Description = dto.Description,
                CreatedById = CurrentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

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

                receipt.Lines.Add(new ExportReceiptLine
                {
                    ProductId = lineDto.ProductId,
                    ProductVariantId = lineDto.ProductVariantId,
                    Quantity = lineDto.Quantity,
                    UnitId = lineDto.UnitId,
                    BaseQuantity = baseQty,
                    ConversionRate = convRate,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _dbContext.ExportReceipts.Add(receipt);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "ExportReceipt",
                receipt.Id.ToString(),
                "Created",
                CurrentUserId,
                null,
                JsonSerializer.Serialize(new { receipt.ReceiptNo, receipt.WarehouseId }),
                ClientIpAddress,
                ClientUserAgent
            );

            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, new { id = receipt.Id, receiptNo = receipt.ReceiptNo });
        }

        [TaskManagement.API.Filters.RequireProjectMembership] // Wait, actually standard authorization applies
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExportReceiptDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageWarehouseReceiptsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var receipt = await _dbContext.ExportReceipts
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
            {
                return NotFound(new { message = "Export receipt not found." });
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

            var oldValue = JsonSerializer.Serialize(new
            {
                receipt.WarehouseId,
                receipt.Description,
                LinesCount = receipt.Lines.Count
            });

            _dbContext.ExportReceiptLines.RemoveRange(receipt.Lines);
            receipt.Lines.Clear();

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

                receipt.Lines.Add(new ExportReceiptLine
                {
                    ProductId = lineDto.ProductId,
                    ProductVariantId = lineDto.ProductVariantId,
                    Quantity = lineDto.Quantity,
                    UnitId = lineDto.UnitId,
                    BaseQuantity = baseQty,
                    ConversionRate = convRate,
                    CreatedAt = DateTime.UtcNow
                });
            }

            receipt.WarehouseId = dto.WarehouseId;
            receipt.Description = dto.Description;
            receipt.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "ExportReceipt",
                receipt.Id.ToString(),
                "Updated",
                CurrentUserId,
                oldValue,
                JsonSerializer.Serialize(new { receipt.WarehouseId }),
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
                await _stockService.ConfirmExportReceiptAsync(id, CurrentUserId);

                await _auditService.LogAsync(
                    "ExportReceipt",
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
                await _stockService.CancelExportReceiptAsync(id, CurrentUserId);

                await _auditService.LogAsync(
                    "ExportReceipt",
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

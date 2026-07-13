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
    [Route("api/inventory/transfer-receipts")]
    public class TransferReceiptsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IStockService _stockService;
        private readonly IUnitConversionService _unitConversionService;
        private readonly IAuditService _auditService;

        public TransferReceiptsController(
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
            [FromQuery] Guid? fromWarehouseId = null,
            [FromQuery] Guid? toWarehouseId = null,
            [FromQuery] ReceiptStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var query = _dbContext.TransferReceipts
                .Include(r => r.FromWarehouse)
                .Include(r => r.ToWarehouse)
                .Include(r => r.CreatedBy)
                .AsQueryable();

            if (fromWarehouseId.HasValue)
            {
                query = query.Where(r => r.FromWarehouseId == fromWarehouseId.Value);
            }
            if (toWarehouseId.HasValue)
            {
                query = query.Where(r => r.ToWarehouseId == toWarehouseId.Value);
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
                .Select(r => new TransferReceiptDto
                {
                    Id = r.Id,
                    ReceiptNo = r.ReceiptNo,
                    FromWarehouseId = r.FromWarehouseId,
                    FromWarehouseName = r.FromWarehouse.Name,
                    FromWarehouseCode = r.FromWarehouse.Code,
                    ToWarehouseId = r.ToWarehouseId,
                    ToWarehouseName = r.ToWarehouse.Name,
                    ToWarehouseCode = r.ToWarehouse.Code,
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

            var r = await _dbContext.TransferReceipts
                .Include(x => x.FromWarehouse)
                .Include(x => x.ToWarehouse)
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
                return NotFound(new { message = "Transfer receipt not found." });
            }

            var dto = new TransferReceiptDto
            {
                Id = r.Id,
                ReceiptNo = r.ReceiptNo,
                FromWarehouseId = r.FromWarehouseId,
                FromWarehouseName = r.FromWarehouse.Name,
                FromWarehouseCode = r.FromWarehouse.Code,
                ToWarehouseId = r.ToWarehouseId,
                ToWarehouseName = r.ToWarehouse.Name,
                ToWarehouseCode = r.ToWarehouse.Code,
                Status = r.Status,
                Description = r.Description,
                CreatedById = r.CreatedById,
                CreatedByName = r.CreatedBy.FullName,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                RowVersion = r.RowVersion,
                Lines = r.Lines.Select(line => new TransferReceiptLineDto
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

            var maxReceiptNo = await _dbContext.TransferReceipts
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
        public async Task<IActionResult> Create([FromBody] CreateTransferReceiptDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageWarehouseReceiptsAsync(CurrentUserId))
            {
                return Forbid();
            }

            if (dto.FromWarehouseId == dto.ToWarehouseId)
            {
                return BadRequest(new { message = "Source and destination warehouses cannot be the same." });
            }

            var fromWarehouse = await _dbContext.Warehouses.FindAsync(dto.FromWarehouseId);
            if (fromWarehouse == null || !fromWarehouse.IsActive)
            {
                return BadRequest(new { message = "Source warehouse not found or is inactive." });
            }

            var toWarehouse = await _dbContext.Warehouses.FindAsync(dto.ToWarehouseId);
            if (toWarehouse == null || !toWarehouse.IsActive)
            {
                return BadRequest(new { message = "Destination warehouse not found or is inactive." });
            }

            var receiptNo = await GenerateReceiptNoAsync("CK");

            var receipt = new TransferReceipt
            {
                ReceiptNo = receiptNo,
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
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

                receipt.Lines.Add(new TransferReceiptLine
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

            _dbContext.TransferReceipts.Add(receipt);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "TransferReceipt",
                receipt.Id.ToString(),
                "Created",
                CurrentUserId,
                null,
                JsonSerializer.Serialize(new { receipt.ReceiptNo, receipt.FromWarehouseId, receipt.ToWarehouseId }),
                ClientIpAddress,
                ClientUserAgent
            );

            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, new { id = receipt.Id, receiptNo = receipt.ReceiptNo });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransferReceiptDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageWarehouseReceiptsAsync(CurrentUserId))
            {
                return Forbid();
            }

            if (dto.FromWarehouseId == dto.ToWarehouseId)
            {
                return BadRequest(new { message = "Source and destination warehouses cannot be the same." });
            }

            var receipt = await _dbContext.TransferReceipts
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
            {
                return NotFound(new { message = "Transfer receipt not found." });
            }

            if (receipt.Status != ReceiptStatus.Draft)
            {
                return BadRequest(new { message = "Only draft receipts can be modified." });
            }

            if (!receipt.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Conflict(new { message = "The record has been modified by another user." });
            }

            var fromWarehouse = await _dbContext.Warehouses.FindAsync(dto.FromWarehouseId);
            if (fromWarehouse == null || !fromWarehouse.IsActive)
            {
                return BadRequest(new { message = "Source warehouse not found or is inactive." });
            }

            var toWarehouse = await _dbContext.Warehouses.FindAsync(dto.ToWarehouseId);
            if (toWarehouse == null || !toWarehouse.IsActive)
            {
                return BadRequest(new { message = "Destination warehouse not found or is inactive." });
            }

            var oldValue = JsonSerializer.Serialize(new
            {
                receipt.FromWarehouseId,
                receipt.ToWarehouseId,
                receipt.Description,
                LinesCount = receipt.Lines.Count
            });

            _dbContext.TransferReceiptLines.RemoveRange(receipt.Lines);
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

                receipt.Lines.Add(new TransferReceiptLine
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

            receipt.FromWarehouseId = dto.FromWarehouseId;
            receipt.ToWarehouseId = dto.ToWarehouseId;
            receipt.Description = dto.Description;
            receipt.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "TransferReceipt",
                receipt.Id.ToString(),
                "Updated",
                CurrentUserId,
                oldValue,
                JsonSerializer.Serialize(new { receipt.FromWarehouseId, receipt.ToWarehouseId }),
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
                await _stockService.ConfirmTransferReceiptAsync(id, CurrentUserId);

                await _auditService.LogAsync(
                    "TransferReceipt",
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
                await _stockService.CancelTransferReceiptAsync(id, CurrentUserId);

                await _auditService.LogAsync(
                    "TransferReceipt",
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

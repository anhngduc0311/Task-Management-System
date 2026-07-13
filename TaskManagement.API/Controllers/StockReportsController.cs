using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Stock;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/inventory")]
    public class StockReportsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;

        public StockReportsController(IAppDbContext dbContext, IPermissionService permissionService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
        }

        [HttpGet("stock-balances")]
        public async Task<IActionResult> GetStockBalances(
            [FromQuery] Guid? warehouseId = null,
            [FromQuery] Guid? productId = null,
            [FromQuery] Guid? productVariantId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var query = _dbContext.StockBalances
                .Include(sb => sb.Warehouse)
                .Include(sb => sb.Product)
                .Include(sb => sb.ProductVariant)
                .AsQueryable();

            if (warehouseId.HasValue)
            {
                query = query.Where(sb => sb.WarehouseId == warehouseId.Value);
            }
            if (productId.HasValue)
            {
                query = query.Where(sb => sb.ProductId == productId.Value);
            }
            if (productVariantId.HasValue)
            {
                query = query.Where(sb => sb.ProductVariantId == productVariantId.Value);
            }

            var total = await query.CountAsync();
            var skip = (page - 1) * pageSize;
            var items = await query
                .OrderBy(sb => sb.Warehouse.Code)
                .ThenBy(sb => sb.Product.ProductCode)
                .Skip(skip)
                .Take(pageSize)
                .Select(sb => new StockBalanceDto
                {
                    Id = sb.Id,
                    WarehouseId = sb.WarehouseId,
                    WarehouseName = sb.Warehouse.Name,
                    WarehouseCode = sb.Warehouse.Code,
                    ProductId = sb.ProductId,
                    ProductName = sb.Product.Name,
                    ProductCode = sb.Product.ProductCode,
                    ProductVariantId = sb.ProductVariantId,
                    VariantSKU = sb.ProductVariant != null ? sb.ProductVariant.SKU : null,
                    Quantity = sb.Quantity,
                    LastUpdatedAt = sb.LastUpdatedAt
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("stock-movements")]
        public async Task<IActionResult> GetStockMovements(
            [FromQuery] Guid? warehouseId = null,
            [FromQuery] Guid? productId = null,
            [FromQuery] MovementType? movementType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var query = _dbContext.StockMovements
                .Include(sm => sm.Warehouse)
                .Include(sm => sm.Product)
                .Include(sm => sm.ProductVariant)
                .AsQueryable();

            if (warehouseId.HasValue)
            {
                query = query.Where(sm => sm.WarehouseId == warehouseId.Value);
            }
            if (productId.HasValue)
            {
                query = query.Where(sm => sm.ProductId == productId.Value);
            }
            if (movementType.HasValue)
            {
                query = query.Where(sm => sm.MovementType == movementType.Value);
            }
            if (startDate.HasValue)
            {
                query = query.Where(sm => sm.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(sm => sm.CreatedAt <= endDate.Value);
            }

            var total = await query.CountAsync();
            var skip = (page - 1) * pageSize;
            var items = await query
                .OrderByDescending(sm => sm.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(sm => new StockMovementDto
                {
                    Id = sm.Id,
                    WarehouseId = sm.WarehouseId,
                    WarehouseName = sm.Warehouse.Name,
                    WarehouseCode = sm.Warehouse.Code,
                    ProductId = sm.ProductId,
                    ProductName = sm.Product.Name,
                    ProductCode = sm.Product.ProductCode,
                    ProductVariantId = sm.ProductVariantId,
                    VariantSKU = sm.ProductVariant != null ? sm.ProductVariant.SKU : null,
                    Quantity = sm.Quantity,
                    MovementType = sm.MovementType,
                    ReferenceId = sm.ReferenceId,
                    ReferenceNo = sm.ReferenceNo,
                    CreatedAt = sm.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("products/{productId}/stock")]
        public async Task<IActionResult> GetProductStock(Guid productId)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var productExists = await _dbContext.Products.AnyAsync(p => p.Id == productId && !p.IsDeleted);
            if (!productExists)
            {
                return NotFound(new { message = "Product not found." });
            }

            var items = await _dbContext.StockBalances
                .Include(sb => sb.Warehouse)
                .Include(sb => sb.Product)
                .Include(sb => sb.ProductVariant)
                .Where(sb => sb.ProductId == productId)
                .OrderBy(sb => sb.Warehouse.Code)
                .Select(sb => new StockBalanceDto
                {
                    Id = sb.Id,
                    WarehouseId = sb.WarehouseId,
                    WarehouseName = sb.Warehouse.Name,
                    WarehouseCode = sb.Warehouse.Code,
                    ProductId = sb.ProductId,
                    ProductName = sb.Product.Name,
                    ProductCode = sb.Product.ProductCode,
                    ProductVariantId = sb.ProductVariantId,
                    VariantSKU = sb.ProductVariant != null ? sb.ProductVariant.SKU : null,
                    Quantity = sb.Quantity,
                    LastUpdatedAt = sb.LastUpdatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("warehouses/{warehouseId}/stock")]
        public async Task<IActionResult> GetWarehouseStock(Guid warehouseId)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == warehouseId);
            if (!warehouseExists)
            {
                return NotFound(new { message = "Warehouse not found." });
            }

            var items = await _dbContext.StockBalances
                .Include(sb => sb.Warehouse)
                .Include(sb => sb.Product)
                .Include(sb => sb.ProductVariant)
                .Where(sb => sb.WarehouseId == warehouseId)
                .OrderBy(sb => sb.Product.ProductCode)
                .Select(sb => new StockBalanceDto
                {
                    Id = sb.Id,
                    WarehouseId = sb.WarehouseId,
                    WarehouseName = sb.Warehouse.Name,
                    WarehouseCode = sb.Warehouse.Code,
                    ProductId = sb.ProductId,
                    ProductName = sb.Product.Name,
                    ProductCode = sb.Product.ProductCode,
                    ProductVariantId = sb.ProductVariantId,
                    VariantSKU = sb.ProductVariant != null ? sb.ProductVariant.SKU : null,
                    Quantity = sb.Quantity,
                    LastUpdatedAt = sb.LastUpdatedAt
                })
                .ToListAsync();

            return Ok(items);
        }
    }
}

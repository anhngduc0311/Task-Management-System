using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.ProductUnitConversions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/products/{productId}/unit-conversions")]
    public class ProductUnitConversionsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public ProductUnitConversionsController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByProduct(Guid productId)
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

            var conversions = await _dbContext.ProductUnitConversions
                .Include(c => c.FromUnit)
                .Include(c => c.ToUnit)
                .Where(c => c.ProductId == productId)
                .Select(c => new ConversionDto
                {
                    Id = c.Id,
                    ProductId = c.ProductId,
                    FromUnitId = c.FromUnitId,
                    FromUnitName = c.FromUnit.Name,
                    ToUnitId = c.ToUnitId,
                    ToUnitName = c.ToUnit.Name,
                    ConversionRate = c.ConversionRate
                })
                .ToListAsync();

            return Ok(conversions);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid productId, [FromBody] CreateConversionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null || product.IsDeleted)
            {
                return NotFound(new { message = "Product not found." });
            }

            // ToUnitId must be the base unit of the product
            if (dto.ToUnitId != product.BaseUnitId)
            {
                return BadRequest(new { message = $"ToUnitId must match the product's base unit ({product.BaseUnitId})." });
            }

            // Cannot convert from base unit to base unit
            if (dto.FromUnitId == product.BaseUnitId)
            {
                return BadRequest(new { message = "Cannot create conversion from base unit to itself." });
            }

            // Check if conversion already exists
            var exists = await _dbContext.ProductUnitConversions
                .AnyAsync(c => c.ProductId == productId && c.FromUnitId == dto.FromUnitId);
            if (exists)
            {
                return BadRequest(new { message = "Conversion for this unit already exists on the product." });
            }

            // Check if units exist
            var fromUnitExists = await _dbContext.Units.AnyAsync(u => u.Id == dto.FromUnitId);
            if (!fromUnitExists)
            {
                return BadRequest(new { message = "FromUnit not found." });
            }

            var conversion = new ProductUnitConversion
            {
                ProductId = productId,
                FromUnitId = dto.FromUnitId,
                ToUnitId = dto.ToUnitId,
                ConversionRate = dto.ConversionRate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.ProductUnitConversions.Add(conversion);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductUnitConversion",
                entityId: conversion.Id.ToString(),
                action: "UnitConversionCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            // Fetch names for returning DTO
            var fromUnitName = await _dbContext.Units.Where(u => u.Id == dto.FromUnitId).Select(u => u.Name).FirstAsync();
            var toUnitName = await _dbContext.Units.Where(u => u.Id == dto.ToUnitId).Select(u => u.Name).FirstAsync();

            var result = new ConversionDto
            {
                Id = conversion.Id,
                ProductId = conversion.ProductId,
                FromUnitId = conversion.FromUnitId,
                FromUnitName = fromUnitName,
                ToUnitId = conversion.ToUnitId,
                ToUnitName = toUnitName,
                ConversionRate = conversion.ConversionRate
            };

            return Created($"/api/products/{productId}/unit-conversions/{conversion.Id}", result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateConversionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null || product.IsDeleted)
            {
                return NotFound(new { message = "Product not found." });
            }

            var conversion = await _dbContext.ProductUnitConversions.FindAsync(id);
            if (conversion == null || conversion.ProductId != productId)
            {
                return NotFound(new { message = "Conversion not found." });
            }

            // ToUnitId must be the base unit of the product
            if (dto.ToUnitId != product.BaseUnitId)
            {
                return BadRequest(new { message = $"ToUnitId must match the product's base unit ({product.BaseUnitId})." });
            }

            // Cannot convert from base unit to base unit
            if (dto.FromUnitId == product.BaseUnitId)
            {
                return BadRequest(new { message = "Cannot create conversion from base unit to itself." });
            }

            // Check duplicate
            var exists = await _dbContext.ProductUnitConversions
                .AnyAsync(c => c.Id != id && c.ProductId == productId && c.FromUnitId == dto.FromUnitId);
            if (exists)
            {
                return BadRequest(new { message = "Conversion for this unit already exists on the product." });
            }

            var oldValue = JsonSerializer.Serialize(new { conversion.FromUnitId, conversion.ToUnitId, conversion.ConversionRate });

            conversion.FromUnitId = dto.FromUnitId;
            conversion.ToUnitId = dto.ToUnitId;
            conversion.ConversionRate = dto.ConversionRate;
            conversion.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductUnitConversion",
                entityId: conversion.Id.ToString(),
                action: "UnitConversionUpdated",
                changedById: CurrentUserId,
                oldValue: oldValue,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid productId, Guid id)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var conversion = await _dbContext.ProductUnitConversions.FindAsync(id);
            if (conversion == null || conversion.ProductId != productId)
            {
                return NotFound(new { message = "Conversion not found." });
            }

            // Restrict if this unit is used in warehouse document lines for this product
            var inImports = await _dbContext.ImportReceiptLines.AnyAsync(l => l.ProductId == productId && l.UnitId == conversion.FromUnitId);
            var inExports = await _dbContext.ExportReceiptLines.AnyAsync(l => l.ProductId == productId && l.UnitId == conversion.FromUnitId);
            var inTransfers = await _dbContext.TransferReceiptLines.AnyAsync(l => l.ProductId == productId && l.UnitId == conversion.FromUnitId);

            if (inImports || inExports || inTransfers)
            {
                return BadRequest(new { message = "Cannot delete conversion as it is currently in use in warehouse receipts." });
            }

            var oldValue = JsonSerializer.Serialize(new { conversion.FromUnitId, conversion.ToUnitId, conversion.ConversionRate });

            _dbContext.ProductUnitConversions.Remove(conversion);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductUnitConversion",
                entityId: id.ToString(),
                action: "UnitConversionDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }
    }
}

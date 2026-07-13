using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.ProductVariants;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/products/{productId}/variants")]
    public class ProductVariantsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public ProductVariantsController(
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

            var variants = await _dbContext.ProductVariants
                .Include(v => v.VariantAttributeValues)
                    .ThenInclude(vav => vav.ProductAttributeValue)
                .Where(v => v.ProductId == productId && !v.IsDeleted)
                .ToListAsync();

            var result = variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                ProductId = v.ProductId,
                SKU = v.SKU,
                Price = v.Price,
                ImageUrl = v.ImageUrl,
                AttributeValueIds = v.VariantAttributeValues.Select(vav => vav.ProductAttributeValueId).ToList(),
                AttributeValueCombinations = string.Join(" / ", v.VariantAttributeValues.Select(vav => vav.ProductAttributeValue.Value)),
                RowVersion = Convert.ToBase64String(v.RowVersion)
            }).ToList();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid productId, [FromBody] CreateProductVariantDto dto)
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

            // SKU unique across all products
            var skuExists = await _dbContext.ProductVariants.AnyAsync(v => v.SKU.ToUpper() == dto.SKU.ToUpper() && !v.IsDeleted);
            if (skuExists)
            {
                return BadRequest(new { message = $"SKU '{dto.SKU}' already exists." });
            }

            // Verify price
            if (dto.Price.HasValue && dto.Price.Value < 0)
            {
                return BadRequest(new { message = "Price cannot be negative." });
            }

            // Verify attribute values exist and belong to this product
            if (dto.AttributeValueIds == null || dto.AttributeValueIds.Count == 0)
            {
                return BadRequest(new { message = "At least one attribute value is required to create a variant." });
            }

            var values = await _dbContext.ProductAttributeValues
                .Include(v => v.AttributeGroup)
                .Where(v => dto.AttributeValueIds.Contains(v.Id) && v.AttributeGroup.ProductId == productId)
                .ToListAsync();

            if (values.Count != dto.AttributeValueIds.Count)
            {
                return BadRequest(new { message = "Some attribute values are invalid or do not belong to this product." });
            }

            // Check if combination already exists
            var existingVariants = await _dbContext.ProductVariants
                .Include(v => v.VariantAttributeValues)
                .Where(v => v.ProductId == productId && !v.IsDeleted)
                .ToListAsync();

            var combinationExists = existingVariants.Any(v =>
                v.VariantAttributeValues.Count == dto.AttributeValueIds.Count &&
                v.VariantAttributeValues.All(vav => dto.AttributeValueIds.Contains(vav.ProductAttributeValueId)));

            if (combinationExists)
            {
                return BadRequest(new { message = "A variant with this combination of attributes already exists." });
            }

            var variant = new ProductVariant
            {
                ProductId = productId,
                SKU = dto.SKU.Trim(),
                Price = dto.Price,
                ImageUrl = dto.ImageUrl?.Trim(),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var val in values)
            {
                variant.VariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    ProductAttributeValueId = val.Id
                });
            }

            _dbContext.ProductVariants.Add(variant);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductVariant",
                entityId: variant.Id.ToString(),
                action: "ProductVariantCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var result = new ProductVariantDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                SKU = variant.SKU,
                Price = variant.Price,
                ImageUrl = variant.ImageUrl,
                AttributeValueIds = dto.AttributeValueIds,
                AttributeValueCombinations = string.Join(" / ", values.Select(v => v.Value)),
                RowVersion = Convert.ToBase64String(variant.RowVersion)
            };

            return Created($"/api/products/{productId}/variants/{variant.Id}", result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateProductVariantDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var variant = await _dbContext.ProductVariants.FindAsync(id);
            if (variant == null || variant.ProductId != productId || variant.IsDeleted)
            {
                return NotFound(new { message = "Variant not found." });
            }

            // Concurrency check
            var clientRowVersion = Convert.FromBase64String(dto.RowVersion);
            if (!variant.RowVersion.SequenceEqual(clientRowVersion))
            {
                return Conflict(new { message = "Concurrency conflict. The variant has been modified by another process." });
            }

            var oldValue = JsonSerializer.Serialize(new { variant.Price, variant.ImageUrl });

            variant.Price = dto.Price;
            variant.ImageUrl = dto.ImageUrl?.Trim();
            variant.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Concurrency conflict." });
            }

            await _auditService.LogAsync(
                entityType: "ProductVariant",
                entityId: variant.Id.ToString(),
                action: "ProductVariantUpdated",
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

            var variant = await _dbContext.ProductVariants.FindAsync(id);
            if (variant == null || variant.ProductId != productId || variant.IsDeleted)
            {
                return NotFound(new { message = "Variant not found." });
            }

            // Restrict if variant has stock transactions or is referenced in warehouse documents
            var inStocks = await _dbContext.StockBalances.AnyAsync(sb => sb.ProductVariantId == id);
            var inMovements = await _dbContext.StockMovements.AnyAsync(sm => sm.ProductVariantId == id);
            var inImports = await _dbContext.ImportReceiptLines.AnyAsync(l => l.ProductVariantId == id);
            var inExports = await _dbContext.ExportReceiptLines.AnyAsync(l => l.ProductVariantId == id);
            var inTransfers = await _dbContext.TransferReceiptLines.AnyAsync(l => l.ProductVariantId == id);

            if (inStocks || inMovements || inImports || inExports || inTransfers)
            {
                return BadRequest(new { message = "Cannot delete variant as it is referenced in stock balances or warehouse documents." });
            }

            var oldValue = JsonSerializer.Serialize(new { variant.SKU, variant.Price, variant.ImageUrl });

            variant.IsDeleted = true;
            variant.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductVariant",
                entityId: id.ToString(),
                action: "ProductVariantDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        // Auto-generator: POST api/products/{productId}/variants/generate
        [HttpPost("generate")]
        public async Task<IActionResult> Generate(Guid productId)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var product = await _dbContext.Products
                .Include(p => p.AttributeGroups)
                    .ThenInclude(g => g.AttributeValues)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            if (product.AttributeGroups.Count == 0)
            {
                return BadRequest(new { message = "No attribute groups found for this product. Please define them first." });
            }

            // Fetch existing active variants to avoid duplicate generation
            var existingVariants = await _dbContext.ProductVariants
                .Include(v => v.VariantAttributeValues)
                .Where(v => v.ProductId == productId && !v.IsDeleted)
                .ToListAsync();

            // Generate combinations
            List<List<ProductAttributeValue>> combinations = new List<List<ProductAttributeValue>>();

            var groups = product.AttributeGroups.OrderBy(g => g.DisplayOrder).ToList();
            if (groups.Count == 1)
            {
                foreach (var val in groups[0].AttributeValues)
                {
                    combinations.Add(new List<ProductAttributeValue> { val });
                }
            }
            else if (groups.Count == 2)
            {
                foreach (var val1 in groups[0].AttributeValues)
                {
                    foreach (var val2 in groups[1].AttributeValues)
                    {
                        combinations.Add(new List<ProductAttributeValue> { val1, val2 });
                    }
                }
            }

            int generatedCount = 0;
            var createdVariants = new List<ProductVariantDto>();

            foreach (var combo in combinations)
            {
                // Check if already exists
                var exists = existingVariants.Any(v =>
                    v.VariantAttributeValues.Count == combo.Count &&
                    v.VariantAttributeValues.All(vav => combo.Any(c => c.Id == vav.ProductAttributeValueId)));

                if (exists) continue;

                // Generate SKU: [ProductCode]-[Value1]-[Value2]
                var cleanValues = combo.Select(v => CleanStringForSKU(v.Value));
                var sku = $"{product.ProductCode}-{string.Join("-", cleanValues)}".ToUpper();

                // Make sure SKU is unique globally
                var baseSku = sku;
                int suffix = 1;
                while (await _dbContext.ProductVariants.AnyAsync(v => v.SKU == sku && !v.IsDeleted))
                {
                    sku = $"{baseSku}-{suffix}";
                    suffix++;
                }

                var variant = new ProductVariant
                {
                    ProductId = productId,
                    SKU = sku,
                    Price = null, // Default is null, inherits from product
                    ImageUrl = null,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                foreach (var val in combo)
                {
                    variant.VariantAttributeValues.Add(new ProductVariantAttributeValue
                    {
                        ProductAttributeValueId = val.Id
                    });
                }

                _dbContext.ProductVariants.Add(variant);
                generatedCount++;

                createdVariants.Add(new ProductVariantDto
                {
                    Id = variant.Id,
                    ProductId = variant.ProductId,
                    SKU = variant.SKU,
                    Price = variant.Price,
                    ImageUrl = variant.ImageUrl,
                    AttributeValueIds = combo.Select(c => c.Id).ToList(),
                    AttributeValueCombinations = string.Join(" / ", combo.Select(c => c.Value))
                });
            }

            if (generatedCount > 0)
            {
                await _dbContext.SaveChangesAsync();

                // Update RowVersions in created DTOs since save changes populated them
                foreach (var vDto in createdVariants)
                {
                    var entity = await _dbContext.ProductVariants.FindAsync(vDto.Id);
                    if (entity != null)
                    {
                        vDto.RowVersion = Convert.ToBase64String(entity.RowVersion);
                    }
                }

                await _auditService.LogAsync(
                    entityType: "ProductVariant",
                    entityId: productId.ToString(),
                    action: "ProductVariantsAutoGenerated",
                    changedById: CurrentUserId,
                    newValue: $"Generated {generatedCount} variants.",
                    ipAddress: ClientIpAddress,
                    userAgent: ClientUserAgent
                );
            }

            return Ok(new { message = $"Successfully generated {generatedCount} new variants.", variants = createdVariants });
        }

        private string CleanStringForSKU(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Clean up to keep alphanumeric characters
            var cleaned = Regex.Replace(input, @"[^a-zA-Z0-9]", "");
            return cleaned;
        }
    }
}

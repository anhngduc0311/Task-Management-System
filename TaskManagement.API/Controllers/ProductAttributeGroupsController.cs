using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Products;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/products/{productId}/attribute-groups")]
    public class ProductAttributeGroupsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public ProductAttributeGroupsController(
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

            var groups = await _dbContext.ProductAttributeGroups
                .Include(g => g.AttributeValues)
                .Where(g => g.ProductId == productId)
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new ProductAttributeGroupDto
                {
                    Id = g.Id,
                    ProductId = g.ProductId,
                    Name = g.Name,
                    DisplayOrder = g.DisplayOrder,
                    Values = g.AttributeValues
                        .OrderBy(v => v.DisplayOrder)
                        .Select(v => new ProductAttributeValueDto
                        {
                            Id = v.Id,
                            AttributeGroupId = v.AttributeGroupId,
                            Value = v.Value,
                            DisplayOrder = v.DisplayOrder
                        }).ToList()
                })
                .ToListAsync();

            return Ok(groups);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid productId, [FromBody] CreateProductAttributeGroupDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var product = await _dbContext.Products
                .Include(p => p.AttributeGroups)
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            // A product can have a maximum of 2 attribute groups
            if (product.AttributeGroups.Count >= 2)
            {
                return BadRequest(new { message = "A product can have a maximum of 2 attribute groups." });
            }

            // Check duplicate group name for this product
            var duplicateName = product.AttributeGroups.Any(g => g.Name.ToUpper() == dto.Name.ToUpper());
            if (duplicateName)
            {
                return BadRequest(new { message = $"Attribute group '{dto.Name}' already exists for this product." });
            }

            var group = new ProductAttributeGroup
            {
                ProductId = productId,
                Name = dto.Name.Trim(),
                DisplayOrder = dto.DisplayOrder
            };

            if (dto.Values != null && dto.Values.Count > 0)
            {
                // Validate unique values
                var uniqueValues = dto.Values.Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                for (int i = 0; i < uniqueValues.Count; i++)
                {
                    group.AttributeValues.Add(new ProductAttributeValue
                    {
                        Value = uniqueValues[i],
                        DisplayOrder = i
                    });
                }
            }

            _dbContext.ProductAttributeGroups.Add(group);
            product.AddAttributeGroup(group);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductAttributeGroup",
                entityId: group.Id.ToString(),
                action: "AttributeGroupCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var result = new ProductAttributeGroupDto
            {
                Id = group.Id,
                ProductId = group.ProductId,
                Name = group.Name,
                DisplayOrder = group.DisplayOrder,
                Values = group.AttributeValues.Select(v => new ProductAttributeValueDto
                {
                    Id = v.Id,
                    AttributeGroupId = v.AttributeGroupId,
                    Value = v.Value,
                    DisplayOrder = v.DisplayOrder
                }).ToList()
            };

            return Created($"/api/products/{productId}/attribute-groups/{group.Id}", result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid productId, Guid id, [FromBody] UpdateProductAttributeGroupDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var group = await _dbContext.ProductAttributeGroups.FindAsync(id);
            if (group == null || group.ProductId != productId)
            {
                return NotFound(new { message = "Attribute group not found." });
            }

            // Check duplicate group name for this product
            var duplicateName = await _dbContext.ProductAttributeGroups
                .AnyAsync(g => g.Id != id && g.ProductId == productId && g.Name.ToUpper() == dto.Name.ToUpper());
            if (duplicateName)
            {
                return BadRequest(new { message = $"Attribute group '{dto.Name}' already exists for this product." });
            }

            var oldValue = JsonSerializer.Serialize(new { group.Name, group.DisplayOrder });

            group.Name = dto.Name.Trim();
            group.DisplayOrder = dto.DisplayOrder;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductAttributeGroup",
                entityId: group.Id.ToString(),
                action: "AttributeGroupUpdated",
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

            var group = await _dbContext.ProductAttributeGroups
                .Include(g => g.AttributeValues)
                .FirstOrDefaultAsync(g => g.Id == id && g.ProductId == productId);

            if (group == null)
            {
                return NotFound(new { message = "Attribute group not found." });
            }

            // Warn: removing group will cascade delete all attributes & values and their associated variants
            var oldValue = JsonSerializer.Serialize(new { group.Name, group.DisplayOrder });

            _dbContext.ProductAttributeGroups.Remove(group);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductAttributeGroup",
                entityId: id.ToString(),
                action: "AttributeGroupDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        // Add Value to Group
        [HttpPost("{groupId}/values")]
        public async Task<IActionResult> AddValue(Guid productId, Guid groupId, [FromBody] CreateProductAttributeValueDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var group = await _dbContext.ProductAttributeGroups
                .Include(g => g.AttributeValues)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.ProductId == productId);

            if (group == null)
            {
                return NotFound(new { message = "Attribute group not found." });
            }

            // Check duplicate value in group
            var exists = group.AttributeValues.Any(v => v.Value.ToUpper() == dto.Value.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = $"Value '{dto.Value}' already exists in this group." });
            }

            var val = new ProductAttributeValue
            {
                AttributeGroupId = groupId,
                Value = dto.Value.Trim(),
                DisplayOrder = dto.DisplayOrder
            };

            _dbContext.ProductAttributeValues.Add(val);
            group.AttributeValues.Add(val);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductAttributeValue",
                entityId: val.Id.ToString(),
                action: "AttributeValueCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var result = new ProductAttributeValueDto
            {
                Id = val.Id,
                AttributeGroupId = val.AttributeGroupId,
                Value = val.Value,
                DisplayOrder = val.DisplayOrder
            };

            return Created($"/api/products/{productId}/attribute-groups/{groupId}/values/{val.Id}", result);
        }

        // Delete Value
        [HttpDelete("/api/products/{productId}/attribute-values/{valueId}")]
        public async Task<IActionResult> DeleteValue(Guid productId, Guid valueId)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var value = await _dbContext.ProductAttributeValues
                .Include(v => v.AttributeGroup)
                .FirstOrDefaultAsync(v => v.Id == valueId && v.AttributeGroup.ProductId == productId);

            if (value == null)
            {
                return NotFound(new { message = "Attribute value not found." });
            }

            // Warn: removing value will cascade delete it and disassociate any variants that use it
            var oldValue = JsonSerializer.Serialize(new { value.Value, value.DisplayOrder });

            _dbContext.ProductAttributeValues.Remove(value);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductAttributeValue",
                entityId: valueId.ToString(),
                action: "AttributeValueDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }
    }
}

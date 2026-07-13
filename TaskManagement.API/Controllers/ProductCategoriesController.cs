using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.ProductCategories;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/product-categories")]
    public class ProductCategoriesController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public ProductCategoriesController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTree()
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var categories = await _dbContext.ProductCategories.ToListAsync();
            var tree = BuildTree(categories);
            return Ok(tree);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var category = await _dbContext.ProductCategories
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            var dto = new CategoryDto
            {
                Id = category.Id,
                ParentId = category.ParentId,
                ParentName = category.Parent?.Name,
                Code = category.Code,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder
            };

            return Ok(dto);
        }

        [HttpGet("{id}/children")]
        public async Task<IActionResult> GetChildren(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var children = await _dbContext.ProductCategories
                .Where(c => c.ParentId == id)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    ParentId = c.ParentId,
                    ParentName = c.Parent != null ? c.Parent.Name : null,
                    Code = c.Code,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    DisplayOrder = c.DisplayOrder
                })
                .ToListAsync();

            return Ok(children);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            // Check duplicate Code
            var exists = await _dbContext.ProductCategories.AnyAsync(c => c.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Category code already exists." });
            }

            var category = new ProductCategory
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder
            };

            if (dto.ParentId.HasValue)
            {
                var allCategories = await _dbContext.ProductCategories.ToListAsync();
                try
                {
                    category.UpdateParent(dto.ParentId.Value, allCategories);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            _dbContext.ProductCategories.Add(category);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductCategory",
                entityId: category.Id.ToString(),
                action: "CategoryCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var result = new CategoryDto
            {
                Id = category.Id,
                ParentId = category.ParentId,
                Code = category.Code,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder
            };

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var category = await _dbContext.ProductCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            // Check duplicate Code
            var exists = await _dbContext.ProductCategories.AnyAsync(c => c.Id != id && c.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Category code already exists." });
            }

            var oldValue = JsonSerializer.Serialize(new 
            { 
                category.ParentId, 
                category.Code, 
                category.Name, 
                category.Description, 
                category.IsActive, 
                category.DisplayOrder 
            });

            category.Code = dto.Code.Trim();
            category.Name = dto.Name.Trim();
            category.Description = dto.Description?.Trim();
            category.IsActive = dto.IsActive;
            category.DisplayOrder = dto.DisplayOrder;

            var allCategories = await _dbContext.ProductCategories.ToListAsync();
            try
            {
                category.UpdateParent(dto.ParentId, allCategories);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductCategory",
                entityId: category.Id.ToString(),
                action: "CategoryUpdated",
                changedById: CurrentUserId,
                oldValue: oldValue,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var category = await _dbContext.ProductCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            // Restrict if category has associated products (which are not deleted)
            var hasProducts = await _dbContext.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted);
            if (hasProducts)
            {
                return BadRequest(new { message = "Cannot delete category as it is currently associated with active products." });
            }

            // Restrict if category has child categories
            var hasChildren = await _dbContext.ProductCategories.AnyAsync(c => c.ParentId == id);
            if (hasChildren)
            {
                return BadRequest(new { message = "Cannot delete category as it has sub-categories." });
            }

            var oldValue = JsonSerializer.Serialize(new 
            { 
                category.ParentId, 
                category.Code, 
                category.Name, 
                category.Description, 
                category.IsActive, 
                category.DisplayOrder 
            });

            _dbContext.ProductCategories.Remove(category);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductCategory",
                entityId: id.ToString(),
                action: "CategoryDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        private List<CategoryNodeDto> BuildTree(List<ProductCategory> allCategories)
        {
            var lookup = allCategories.ToLookup(c => c.ParentId);

            List<CategoryNodeDto> MapNode(Guid? parentId)
            {
                return lookup[parentId]
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => new CategoryNodeDto
                    {
                        Id = c.Id,
                        ParentId = c.ParentId,
                        Code = c.Code,
                        Name = c.Name,
                        Description = c.Description,
                        IsActive = c.IsActive,
                        DisplayOrder = c.DisplayOrder,
                        Children = MapNode(c.Id)
                    })
                    .ToList();
            }

            return MapNode(null);
        }
    }
}

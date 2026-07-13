using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.ProductLabels;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/product-labels")]
    public class ProductLabelsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public ProductLabelsController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var labels = await _dbContext.ProductLabels
                .OrderBy(l => l.Name)
                .Select(l => new LabelDto
                {
                    Id = l.Id,
                    Code = l.Code,
                    Name = l.Name,
                    Color = l.Color,
                    IsActive = l.IsActive
                })
                .ToListAsync();

            return Ok(labels);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var label = await _dbContext.ProductLabels.FindAsync(id);
            if (label == null)
            {
                return NotFound(new { message = "Label not found." });
            }

            var dto = new LabelDto
            {
                Id = label.Id,
                Code = label.Code,
                Name = label.Name,
                Color = label.Color,
                IsActive = label.IsActive
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLabelDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var exists = await _dbContext.ProductLabels.AnyAsync(l => l.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Label code already exists." });
            }

            var label = new ProductLabel
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Color = dto.Color?.Trim(),
                IsActive = dto.IsActive
            };

            _dbContext.ProductLabels.Add(label);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductLabel",
                entityId: label.Id.ToString(),
                action: "LabelCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var result = new LabelDto
            {
                Id = label.Id,
                Code = label.Code,
                Name = label.Name,
                Color = label.Color,
                IsActive = label.IsActive
            };

            return CreatedAtAction(nameof(GetById), new { id = label.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLabelDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var label = await _dbContext.ProductLabels.FindAsync(id);
            if (label == null)
            {
                return NotFound(new { message = "Label not found." });
            }

            var exists = await _dbContext.ProductLabels.AnyAsync(l => l.Id != id && l.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Label code already exists." });
            }

            var oldValue = JsonSerializer.Serialize(new { label.Code, label.Name, label.Color, label.IsActive });

            label.Code = dto.Code.Trim();
            label.Name = dto.Name.Trim();
            label.Color = dto.Color?.Trim();
            label.IsActive = dto.IsActive;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductLabel",
                entityId: label.Id.ToString(),
                action: "LabelUpdated",
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

            var label = await _dbContext.ProductLabels.FindAsync(id);
            if (label == null)
            {
                return NotFound(new { message = "Label not found." });
            }

            var oldValue = JsonSerializer.Serialize(new { label.Code, label.Name, label.Color, label.IsActive });

            _dbContext.ProductLabels.Remove(label);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductLabel",
                entityId: id.ToString(),
                action: "LabelDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }
    }
}

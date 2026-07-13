using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Origins;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OriginsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public OriginsController(
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

            var origins = await _dbContext.Origins
                .OrderBy(o => o.Name)
                .Select(o => new OriginDto
                {
                    Id = o.Id,
                    Code = o.Code,
                    Name = o.Name,
                    IsActive = o.IsActive
                })
                .ToListAsync();

            return Ok(origins);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var origin = await _dbContext.Origins.FindAsync(id);
            if (origin == null)
            {
                return NotFound(new { message = "Origin not found." });
            }

            var dto = new OriginDto
            {
                Id = origin.Id,
                Code = origin.Code,
                Name = origin.Name,
                IsActive = origin.IsActive
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOriginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var exists = await _dbContext.Origins.AnyAsync(o => o.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Origin code already exists." });
            }

            var origin = new Origin
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                IsActive = dto.IsActive
            };

            _dbContext.Origins.Add(origin);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Origin",
                entityId: origin.Id.ToString(),
                action: "OriginCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var result = new OriginDto
            {
                Id = origin.Id,
                Code = origin.Code,
                Name = origin.Name,
                IsActive = origin.IsActive
            };

            return CreatedAtAction(nameof(GetById), new { id = origin.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOriginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var origin = await _dbContext.Origins.FindAsync(id);
            if (origin == null)
            {
                return NotFound(new { message = "Origin not found." });
            }

            var exists = await _dbContext.Origins.AnyAsync(o => o.Id != id && o.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Origin code already exists." });
            }

            var oldValue = JsonSerializer.Serialize(new { origin.Code, origin.Name, origin.IsActive });

            origin.Code = dto.Code.Trim();
            origin.Name = dto.Name.Trim();
            origin.IsActive = dto.IsActive;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Origin",
                entityId: origin.Id.ToString(),
                action: "OriginUpdated",
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

            var origin = await _dbContext.Origins.FindAsync(id);
            if (origin == null)
            {
                return NotFound(new { message = "Origin not found." });
            }

            // Check if origin is used by active products
            var inProducts = await _dbContext.Products.AnyAsync(p => p.OriginId == id && !p.IsDeleted);
            if (inProducts)
            {
                return BadRequest(new { message = "Cannot delete origin as it is currently associated with active products." });
            }

            var oldValue = JsonSerializer.Serialize(new { origin.Code, origin.Name, origin.IsActive });

            _dbContext.Origins.Remove(origin);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Origin",
                entityId: id.ToString(),
                action: "OriginDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }
    }
}

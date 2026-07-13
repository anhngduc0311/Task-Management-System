using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Units;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UnitsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public UnitsController(
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

            var units = await _dbContext.Units
                .OrderBy(u => u.Name)
                .Select(u => new UnitDto
                {
                    Id = u.Id,
                    Code = u.Code,
                    Name = u.Name,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return Ok(units);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var unit = await _dbContext.Units.FindAsync(id);
            if (unit == null)
            {
                return NotFound(new { message = "Unit not found." });
            }

            var dto = new UnitDto
            {
                Id = unit.Id,
                Code = unit.Code,
                Name = unit.Name,
                IsActive = unit.IsActive
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUnitDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            // Check duplicate Code
            var exists = await _dbContext.Units.AnyAsync(u => u.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Unit code already exists." });
            }

            var unit = new Unit
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                IsActive = dto.IsActive
            };

            _dbContext.Units.Add(unit);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Unit",
                entityId: unit.Id.ToString(),
                action: "UnitCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var result = new UnitDto
            {
                Id = unit.Id,
                Code = unit.Code,
                Name = unit.Name,
                IsActive = unit.IsActive
            };

            return CreatedAtAction(nameof(GetById), new { id = unit.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var unit = await _dbContext.Units.FindAsync(id);
            if (unit == null)
            {
                return NotFound(new { message = "Unit not found." });
            }

            // Check duplicate Code (excluding current unit)
            var exists = await _dbContext.Units.AnyAsync(u => u.Id != id && u.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Unit code already exists." });
            }

            var oldValue = JsonSerializer.Serialize(new { unit.Code, unit.Name, unit.IsActive });

            unit.Code = dto.Code.Trim();
            unit.Name = dto.Name.Trim();
            unit.IsActive = dto.IsActive;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Unit",
                entityId: unit.Id.ToString(),
                action: "UnitUpdated",
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

            var unit = await _dbContext.Units.FindAsync(id);
            if (unit == null)
            {
                return NotFound(new { message = "Unit not found." });
            }

            // Check if unit is in use
            var inProducts = await _dbContext.Products.AnyAsync(p => p.BaseUnitId == id && !p.IsDeleted);
            var inConversions = await _dbContext.ProductUnitConversions.AnyAsync(c => c.FromUnitId == id || c.ToUnitId == id);
            
            if (inProducts || inConversions)
            {
                return BadRequest(new { message = "Cannot delete unit as it is currently in use by products or unit conversions." });
            }

            var oldValue = JsonSerializer.Serialize(new { unit.Code, unit.Name, unit.IsActive });

            _dbContext.Units.Remove(unit);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Unit",
                entityId: id.ToString(),
                action: "UnitDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }
    }
}

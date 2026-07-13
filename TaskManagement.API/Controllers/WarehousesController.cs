using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Warehouses;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WarehousesController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public WarehousesController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var query = _dbContext.Warehouses.OrderBy(w => w.Code);

            if (page.HasValue && pageSize.HasValue)
            {
                var skip = (page.Value - 1) * pageSize.Value;
                var total = await query.CountAsync();
                var items = await query
                    .Skip(skip)
                    .Take(pageSize.Value)
                    .Select(w => new WarehouseDto
                    {
                        Id = w.Id,
                        Code = w.Code,
                        Name = w.Name,
                        Address = w.Address,
                        Description = w.Description,
                        IsActive = w.IsActive,
                        CreatedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { total, page = page.Value, pageSize = pageSize.Value, items });
            }
            else
            {
                var items = await query
                    .Select(w => new WarehouseDto
                    {
                        Id = w.Id,
                        Code = w.Code,
                        Name = w.Name,
                        Address = w.Address,
                        Description = w.Description,
                        IsActive = w.IsActive,
                        CreatedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(items);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var w = await _dbContext.Warehouses.FindAsync(id);
            if (w == null)
            {
                return NotFound(new { message = "Warehouse not found." });
            }

            var dto = new WarehouseDto
            {
                Id = w.Id,
                Code = w.Code,
                Name = w.Name,
                Address = w.Address,
                Description = w.Description,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var exists = await _dbContext.Warehouses.AnyAsync(w => w.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Warehouse code already exists." });
            }

            var warehouse = new Warehouse
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Address = dto.Address?.Trim(),
                Description = dto.Description?.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Warehouses.Add(warehouse);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "Warehouse",
                warehouse.Id.ToString(),
                "Created",
                CurrentUserId,
                null,
                JsonSerializer.Serialize(dto),
                ClientIpAddress,
                ClientUserAgent
            );

            var resultDto = new WarehouseDto
            {
                Id = warehouse.Id,
                Code = warehouse.Code,
                Name = warehouse.Name,
                Address = warehouse.Address,
                Description = warehouse.Description,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, resultDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var warehouse = await _dbContext.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound(new { message = "Warehouse not found." });
            }

            var exists = await _dbContext.Warehouses.AnyAsync(w => w.Code.ToUpper() == dto.Code.ToUpper() && w.Id != id);
            if (exists)
            {
                return BadRequest(new { message = "Warehouse code already exists." });
            }

            var oldValue = JsonSerializer.Serialize(new
            {
                warehouse.Code,
                warehouse.Name,
                warehouse.Address,
                warehouse.Description,
                warehouse.IsActive
            });

            warehouse.Code = dto.Code.Trim();
            warehouse.Name = dto.Name.Trim();
            warehouse.Address = dto.Address?.Trim();
            warehouse.Description = dto.Description?.Trim();
            warehouse.IsActive = dto.IsActive;
            warehouse.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "Warehouse",
                warehouse.Id.ToString(),
                "Updated",
                CurrentUserId,
                oldValue,
                JsonSerializer.Serialize(dto),
                ClientIpAddress,
                ClientUserAgent
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

            var warehouse = await _dbContext.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound(new { message = "Warehouse not found." });
            }

            // Check if there are associated transactions, movements, or receipts
            var hasBalances = await _dbContext.StockBalances.AnyAsync(sb => sb.WarehouseId == id && sb.Quantity != 0);
            var hasMovements = await _dbContext.StockMovements.AnyAsync(sm => sm.WarehouseId == id);
            var hasImportReceipts = await _dbContext.ImportReceipts.AnyAsync(r => r.WarehouseId == id);
            var hasExportReceipts = await _dbContext.ExportReceipts.AnyAsync(r => r.WarehouseId == id);
            var hasTransferReceipts = await _dbContext.TransferReceipts.AnyAsync(r => r.FromWarehouseId == id || r.ToWarehouseId == id);

            if (hasBalances || hasMovements || hasImportReceipts || hasExportReceipts || hasTransferReceipts)
            {
                return BadRequest(new { message = "Cannot delete warehouse as it has associated inventory transactions or receipts." });
            }

            var oldValue = JsonSerializer.Serialize(new
            {
                warehouse.Code,
                warehouse.Name,
                warehouse.Address,
                warehouse.Description,
                warehouse.IsActive
            });

            _dbContext.Warehouses.Remove(warehouse);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                "Warehouse",
                id.ToString(),
                "Deleted",
                CurrentUserId,
                oldValue,
                null,
                ClientIpAddress,
                ClientUserAgent
            );

            return NoContent();
        }
    }
}

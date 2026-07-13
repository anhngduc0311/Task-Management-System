using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Suppliers;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;

        public SuppliersController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool? isActive)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var query = _dbContext.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpper();
                query = query.Where(s => s.Name.ToUpper().Contains(term) || 
                                         s.Code.ToUpper().Contains(term) || 
                                         (s.Email != null && s.Email.ToUpper().Contains(term)) ||
                                         (s.Phone != null && s.Phone.Contains(term)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            var items = await query
                .OrderBy(s => s.Name)
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Phone = s.Phone,
                    Email = s.Email,
                    Address = s.Address,
                    TaxCode = s.TaxCode,
                    ContactPerson = s.ContactPerson,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var supplier = await _dbContext.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound(new { message = "Supplier not found." });
            }

            var dto = new SupplierDto
            {
                Id = supplier.Id,
                Code = supplier.Code,
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                TaxCode = supplier.TaxCode,
                ContactPerson = supplier.ContactPerson,
                IsActive = supplier.IsActive
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var exists = await _dbContext.Suppliers.AnyAsync(s => s.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Supplier code already exists." });
            }

            var supplier = new Supplier
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                TaxCode = dto.TaxCode?.Trim(),
                ContactPerson = dto.ContactPerson?.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Suppliers.Add(supplier);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Supplier",
                entityId: supplier.Id.ToString(),
                action: "SupplierCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var result = new SupplierDto
            {
                Id = supplier.Id,
                Code = supplier.Code,
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                TaxCode = supplier.TaxCode,
                ContactPerson = supplier.ContactPerson,
                IsActive = supplier.IsActive
            };

            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var supplier = await _dbContext.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound(new { message = "Supplier not found." });
            }

            var exists = await _dbContext.Suppliers.AnyAsync(s => s.Id != id && s.Code.ToUpper() == dto.Code.ToUpper());
            if (exists)
            {
                return BadRequest(new { message = "Supplier code already exists." });
            }

            var oldValue = JsonSerializer.Serialize(new 
            { 
                supplier.Code, 
                supplier.Name, 
                supplier.Phone, 
                supplier.Email, 
                supplier.Address, 
                supplier.TaxCode, 
                supplier.ContactPerson, 
                supplier.IsActive 
            });

            supplier.Code = dto.Code.Trim();
            supplier.Name = dto.Name.Trim();
            supplier.Phone = dto.Phone?.Trim();
            supplier.Email = dto.Email?.Trim();
            supplier.Address = dto.Address?.Trim();
            supplier.TaxCode = dto.TaxCode?.Trim();
            supplier.ContactPerson = dto.ContactPerson?.Trim();
            supplier.IsActive = dto.IsActive;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Supplier",
                entityId: supplier.Id.ToString(),
                action: "SupplierUpdated",
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

            var supplier = await _dbContext.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound(new { message = "Supplier not found." });
            }

            // Check if supplier is used in any active products
            var inProducts = await _dbContext.ProductSuppliers.AnyAsync(ps => ps.SupplierId == id);
            if (inProducts)
            {
                return BadRequest(new { message = "Cannot delete supplier as it is currently associated with active products." });
            }

            var oldValue = JsonSerializer.Serialize(new 
            { 
                supplier.Code, 
                supplier.Name, 
                supplier.Phone, 
                supplier.Email, 
                supplier.Address, 
                supplier.TaxCode, 
                supplier.ContactPerson, 
                supplier.IsActive 
            });

            _dbContext.Suppliers.Remove(supplier);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Supplier",
                entityId: id.ToString(),
                action: "SupplierDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }
    }
}

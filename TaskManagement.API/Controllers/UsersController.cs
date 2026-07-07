using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [Route("api/users")]
    public class UsersController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IAuditService _auditService;

        public UsersController(IAppDbContext dbContext, IAuditService auditService)
        {
            _dbContext = dbContext;
            _auditService = auditService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _dbContext.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(lowerSearch) || u.Email.ToLower().Contains(lowerSearch));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Status = u.Status.ToString(),
                    AvatarUrl = u.AvatarUrl,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                items,
                totalCount,
                page,
                pageSize
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var isSelf = CurrentUserId == id;
            var isAdmin = User.IsInRole("Admin");

            if (!isSelf && !isAdmin)
            {
                return Forbid();
            }

            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var dto = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Status = user.Status.ToString(),
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (CurrentUserId != id)
            {
                return Forbid();
            }

            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var oldUserVal = System.Text.Json.JsonSerializer.Serialize(new { user.FullName, user.AvatarUrl });

            user.FullName = dto.FullName;
            user.AvatarUrl = dto.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var newUserVal = System.Text.Json.JsonSerializer.Serialize(new { user.FullName, user.AvatarUrl });
            await _auditService.LogAsync(
                entityType: "User",
                entityId: user.Id.ToString(),
                action: "ProfileUpdated",
                changedById: CurrentUserId,
                oldValue: oldUserVal,
                newValue: newUserVal,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Status = user.Status.ToString(),
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (id == CurrentUserId)
            {
                return BadRequest(new { message = "Admins cannot disable their own accounts." });
            }

            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (!Enum.TryParse<UserStatus>(dto.Status, out var status))
            {
                return BadRequest(new { message = "Invalid status value." });
            }

            if (user.Status == status)
            {
                return Ok(new { message = $"User is already {status}." });
            }

            var oldStatus = user.Status.ToString();
            user.Status = status;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "User",
                entityId: user.Id.ToString(),
                action: "StatusChanged",
                changedById: CurrentUserId,
                oldValue: oldStatus,
                newValue: status.ToString(),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Status = user.Status.ToString(),
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }
    }
}

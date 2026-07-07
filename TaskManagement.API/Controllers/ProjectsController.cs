using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.API.Filters;
using TaskManagement.Application.DTOs.Projects;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [Route("api/projects")]
    public class ProjectsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public ProjectsController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var isSystemAdmin = User.IsInRole("Admin");
            var query = _dbContext.Projects
                .Include(p => p.Owner)
                .Where(p => p.Status != ProjectStatus.Deleted);

            if (!isSystemAdmin)
            {
                query = query.Where(p => p.OwnerId == CurrentUserId || 
                    p.Members.Any(pm => pm.UserId == CurrentUserId && pm.Status == ProjectMemberStatus.Active));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    OwnerId = p.OwnerId,
                    OwnerFullName = p.Owner.FullName,
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
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

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var isAllowed = User.IsInRole("Admin") || User.IsInRole("ProjectManager");
            if (!isAllowed)
            {
                return Forbid();
            }

            var project = new Project
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    OwnerId = CurrentUserId,
                    Status = ProjectStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

            // Owner is also added as ProjectManager of the project
            var member = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = CurrentUserId,
                    RoleInProject = ProjectMemberRole.ProjectManager,
                    JoinedAt = DateTime.UtcNow,
                    Status = ProjectMemberStatus.Active
                };

            _dbContext.Projects.Add(project);
            _dbContext.ProjectMembers.Add(member);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Project",
                entityId: project.Id.ToString(),
                action: "ProjectCreated",
                changedById: CurrentUserId,
                newValue: System.Text.Json.JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            // Fetch owner name
            var ownerName = await _dbContext.Users
                .Where(u => u.Id == CurrentUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? string.Empty;

            var resultDto = new ProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    OwnerId = project.OwnerId,
                    OwnerFullName = ownerName,
                    Status = project.Status.ToString(),
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt
                };

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, resultDto);
        }

        [HttpGet("{id}")]
        [RequireProjectMembership]
        public async Task<IActionResult> GetProject(Guid id)
        {
            var project = await _dbContext.Projects
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status != ProjectStatus.Deleted);

            if (project == null)
            {
                return NotFound(new { message = "Project not found or deleted." });
            }

            var dto = new ProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    OwnerId = project.OwnerId,
                    OwnerFullName = project.Owner.FullName,
                    Status = project.Status.ToString(),
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt
                };

            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canEdit = await _permissionService.CanEditProjectAsync(CurrentUserId, id);
            if (!canEdit)
            {
                return Forbid();
            }

            var project = await _dbContext.Projects.FindAsync(id);
            if (project == null || project.Status == ProjectStatus.Deleted)
            {
                return NotFound(new { message = "Project not found." });
            }

            if (!Enum.TryParse<ProjectStatus>(dto.Status, out var status))
            {
                return BadRequest(new { message = "Invalid project status." });
            }

            var oldValue = System.Text.Json.JsonSerializer.Serialize(new { project.Name, project.Description, Status = project.Status.ToString() });

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.Status = status;
            project.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var newValue = System.Text.Json.JsonSerializer.Serialize(dto);
            await _auditService.LogAsync(
                entityType: "Project",
                entityId: project.Id.ToString(),
                action: "ProjectUpdated",
                changedById: CurrentUserId,
                oldValue: oldValue,
                newValue: newValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            // Fetch owner name
            var ownerName = await _dbContext.Users
                .Where(u => u.Id == project.OwnerId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? string.Empty;

            return Ok(new ProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    OwnerId = project.OwnerId,
                    OwnerFullName = ownerName,
                    Status = project.Status.ToString(),
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt
                });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var canDelete = await _permissionService.CanDeleteProjectAsync(CurrentUserId, id);
            if (!canDelete)
            {
                return Forbid();
            }

            var project = await _dbContext.Projects.FindAsync(id);
            if (project == null || project.Status == ProjectStatus.Deleted)
            {
                return NotFound(new { message = "Project not found or already deleted." });
            }

            project.Status = ProjectStatus.Deleted;
            project.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Project",
                entityId: project.Id.ToString(),
                action: "ProjectDeleted",
                changedById: CurrentUserId,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Project soft-deleted successfully." });
        }
    }
}

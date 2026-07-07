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
    [Route("api/projects/{projectId}/members")]
    public class ProjectMembersController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public ProjectMembersController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        [HttpGet]
        [RequireProjectMembership]
        public async Task<IActionResult> GetMembers(Guid projectId)
        {
            var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId && p.Status != ProjectStatus.Deleted);
            if (!projectExists)
            {
                return NotFound(new { message = "Project not found." });
            }

            var members = await _dbContext.ProjectMembers
                .Where(pm => pm.ProjectId == projectId && pm.Status == ProjectMemberStatus.Active)
                .Select(pm => new ProjectMemberDto
                {
                    ProjectId = pm.ProjectId,
                    UserId = pm.UserId,
                    FullName = pm.User.FullName,
                    Email = pm.User.Email,
                    RoleInProject = pm.RoleInProject.ToString(),
                    JoinedAt = pm.JoinedAt,
                    Status = pm.Status.ToString()
                })
                .ToListAsync();

            return Ok(members);
        }

        [HttpPost]
        public async Task<IActionResult> AddMember(Guid projectId, [FromBody] AddProjectMemberDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canManage = await _permissionService.CanManageProjectMembersAsync(CurrentUserId, projectId);
            if (!canManage)
            {
                return Forbid();
            }

            var project = await _dbContext.Projects.FindAsync(projectId);
            if (project == null || project.Status == ProjectStatus.Deleted)
            {
                return NotFound(new { message = "Project not found." });
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
            if (user == null || user.Status == UserStatus.Inactive)
            {
                return BadRequest(new { message = "User with this email was not found or is inactive." });
            }

            if (!Enum.TryParse<ProjectMemberRole>(dto.RoleInProject, out var role))
            {
                return BadRequest(new { message = "Invalid project member role." });
            }

            var existingMember = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == user.Id);

            if (existingMember != null)
            {
                if (existingMember.Status == ProjectMemberStatus.Active)
                {
                    return BadRequest(new { message = "User is already an active member of this project." });
                }

                // Reactivate and update role
                var oldRole = existingMember.RoleInProject.ToString();
                existingMember.Status = ProjectMemberStatus.Active;
                existingMember.RoleInProject = role;
                existingMember.JoinedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                await _auditService.LogAsync(
                    entityType: "ProjectMember",
                    entityId: $"{projectId}_{user.Id}",
                    action: "ProjectMemberReactivated",
                    changedById: CurrentUserId,
                    oldValue: oldRole,
                    newValue: role.ToString(),
                    ipAddress: ClientIpAddress,
                    userAgent: ClientUserAgent
                );
            }
            else
            {
                var newMember = new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    RoleInProject = role,
                    JoinedAt = DateTime.UtcNow,
                    Status = ProjectMemberStatus.Active
                };

                _dbContext.ProjectMembers.Add(newMember);
                await _dbContext.SaveChangesAsync();

                await _auditService.LogAsync(
                    entityType: "ProjectMember",
                    entityId: $"{projectId}_{user.Id}",
                    action: "ProjectMemberAdded",
                    changedById: CurrentUserId,
                    newValue: role.ToString(),
                    ipAddress: ClientIpAddress,
                    userAgent: ClientUserAgent
                );
            }

            return Ok(new { message = "Member added successfully." });
        }

        [HttpPut("{uid}")]
        public async Task<IActionResult> UpdateMemberRole(Guid projectId, Guid uid, [FromBody] UpdateProjectMemberRoleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canManage = await _permissionService.CanManageProjectMembersAsync(CurrentUserId, projectId);
            if (!canManage)
            {
                return Forbid();
            }

            var project = await _dbContext.Projects.FindAsync(projectId);
            if (project == null || project.Status == ProjectStatus.Deleted)
            {
                return NotFound(new { message = "Project not found." });
            }

            if (uid == project.OwnerId)
            {
                return BadRequest(new { message = "Cannot modify the role of the project owner." });
            }

            if (!Enum.TryParse<ProjectMemberRole>(dto.RoleInProject, out var role))
            {
                return BadRequest(new { message = "Invalid project member role." });
            }

            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == uid && pm.Status == ProjectMemberStatus.Active);

            if (member == null)
            {
                return NotFound(new { message = "Member not found in project." });
            }

            if (member.RoleInProject == role)
            {
                return Ok(new { message = "Role is already set to this value." });
            }

            var oldRole = member.RoleInProject.ToString();
            member.RoleInProject = role;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProjectMember",
                entityId: $"{projectId}_{uid}",
                action: "ProjectMemberRoleUpdated",
                changedById: CurrentUserId,
                oldValue: oldRole,
                newValue: role.ToString(),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Member role updated successfully." });
        }

        [HttpDelete("{uid}")]
        public async Task<IActionResult> RemoveMember(Guid projectId, Guid uid)
        {
            var canManage = await _permissionService.CanManageProjectMembersAsync(CurrentUserId, projectId);
            if (!canManage)
            {
                return Forbid();
            }

            var project = await _dbContext.Projects.FindAsync(projectId);
            if (project == null || project.Status == ProjectStatus.Deleted)
            {
                return NotFound(new { message = "Project not found." });
            }

            if (uid == project.OwnerId)
            {
                return BadRequest(new { message = "Cannot remove the project owner from the project." });
            }

            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == uid && pm.Status == ProjectMemberStatus.Active);

            if (member == null)
            {
                return NotFound(new { message = "Member not found in project." });
            }

            member.Status = ProjectMemberStatus.Inactive;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProjectMember",
                entityId: $"{projectId}_{uid}",
                action: "ProjectMemberRemoved",
                changedById: CurrentUserId,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Member removed successfully from project." });
        }
    }
}

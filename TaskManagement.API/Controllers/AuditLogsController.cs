using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.AuditLogs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class AuditLogsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;

        public AuditLogsController(IAppDbContext dbContext, IPermissionService permissionService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
        }

        [HttpGet("projects/{projectId}/audit-logs")]
        public async Task<IActionResult> GetProjectAuditLogs(Guid projectId)
        {
            var canView = await _permissionService.CanViewAuditLogAsync(CurrentUserId, projectId);
            if (!canView)
            {
                return Forbid();
            }

            var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId && p.Status != ProjectStatus.Deleted);
            if (!projectExists)
            {
                return NotFound(new { message = "Project not found." });
            }

            var taskIds = await _dbContext.Tasks
                .IgnoreQueryFilters()
                .Where(t => t.ProjectId == projectId)
                .Select(t => t.Id.ToString())
                .ToListAsync();

            var logs = await _dbContext.AuditLogs
                .Where(al =>
                    (al.EntityType == "Project" && al.EntityId == projectId.ToString()) ||
                    (al.EntityType == "ProjectMember" && al.EntityId.StartsWith(projectId.ToString())) ||
                    (al.EntityType == "Task" && taskIds.Contains(al.EntityId))
                )
                .OrderByDescending(al => al.ChangedAt)
                .Select(al => new AuditLogDto
                {
                    Id = al.Id,
                    EntityType = al.EntityType,
                    EntityId = al.EntityId,
                    Action = al.Action,
                    ChangedById = al.ChangedById,
                    ChangedByName = al.ChangedBy.FullName,
                    ChangedAt = al.ChangedAt,
                    OldValue = al.OldValue,
                    NewValue = al.NewValue,
                    IpAddress = al.IpAddress,
                    UserAgent = al.UserAgent
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("tasks/{taskId}/audit-logs")]
        public async Task<IActionResult> GetTaskAuditLogs(Guid taskId)
        {
            var task = await _dbContext.Tasks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            var canView = await _permissionService.CanViewAuditLogAsync(CurrentUserId, task.ProjectId);
            if (!canView)
            {
                return Forbid();
            }

            var commentsQuery = _dbContext.TaskComments
                .IgnoreQueryFilters()
                .Where(tc => tc.TaskId == taskId)
                .Select(tc => tc.Id.ToString());

            var attachmentsQuery = _dbContext.TaskAttachments
                .IgnoreQueryFilters()
                .Where(ta => ta.TaskId == taskId)
                .Select(ta => ta.Id.ToString());

            var logs = await _dbContext.AuditLogs
                .Where(al =>
                    (al.EntityType == "Task" && al.EntityId == taskId.ToString()) ||
                    (al.EntityType == "TaskComment" && commentsQuery.Contains(al.EntityId)) ||
                    (al.EntityType == "TaskAttachment" && attachmentsQuery.Contains(al.EntityId))
                )
                .OrderByDescending(al => al.ChangedAt)
                .Select(al => new AuditLogDto
                {
                    Id = al.Id,
                    EntityType = al.EntityType,
                    EntityId = al.EntityId,
                    Action = al.Action,
                    ChangedById = al.ChangedById,
                    ChangedByName = al.ChangedBy.FullName,
                    ChangedAt = al.ChangedAt,
                    OldValue = al.OldValue,
                    NewValue = al.NewValue,
                    IpAddress = al.IpAddress,
                    UserAgent = al.UserAgent
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.API.Filters;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;
using Task = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class TasksController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;

        public TasksController(
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

        [HttpGet("projects/{projectId}/tasks")]
        [RequireProjectMembership]
        public async Task<IActionResult> GetProjectTasks(
            Guid projectId,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] Guid? assigneeId,
            [FromQuery] DateTime? dueDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var defaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize", 10);
            var maxPageSize = _configuration.GetValue<int>("Pagination:MaxPageSize", 100);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > maxPageSize) pageSize = defaultPageSize;

            var query = _dbContext.Tasks
                .Where(t => t.ProjectId == projectId);

            // Filtering
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(lowerSearch) || 
                    (t.Description != null && t.Description.ToLower().Contains(lowerSearch)));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TaskStatus>(status, out var taskStatus))
            {
                query = query.Where(t => t.Status == taskStatus);
            }

            if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<TaskPriority>(priority, out var taskPriority))
            {
                query = query.Where(t => t.Priority == taskPriority);
            }

            if (assigneeId.HasValue)
            {
                query = query.Where(t => t.AssigneeId == assigneeId.Value);
            }

            if (dueDate.HasValue)
            {
                var targetDate = dueDate.Value.Date;
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value == targetDate);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    AssigneeId = t.AssigneeId,
                    AssigneeName = t.Assignee != null ? t.Assignee.FullName : null,
                    CreatedById = t.CreatedById,
                    CreatedByName = t.CreatedBy.FullName,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    RowVersion = Convert.ToBase64String(t.RowVersion)
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

        [HttpPost("projects/{projectId}/tasks")]
        public async Task<IActionResult> CreateTask(Guid projectId, [FromBody] CreateTaskDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canCreate = await _permissionService.CanCreateTaskAsync(CurrentUserId, projectId);
            if (!canCreate)
            {
                return Forbid();
            }

            var project = await _dbContext.Projects.FindAsync(projectId);
            if (project == null || project.Status == ProjectStatus.Deleted)
            {
                return NotFound(new { message = "Project not found." });
            }

            if (!Enum.TryParse<TaskPriority>(dto.Priority, out var priority))
            {
                return BadRequest(new { message = "Invalid priority level." });
            }

            if (dto.AssigneeId.HasValue)
            {
                var isMember = await _dbContext.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == dto.AssigneeId.Value && pm.Status == ProjectMemberStatus.Active);
                if (!isMember)
                {
                    return BadRequest(new { message = "Assignee must be an active member of this project." });
                }
            }

            var task = new Task
            {
                ProjectId = projectId,
                Title = dto.Title,
                Description = dto.Description,
                Status = TaskStatus.Todo,
                Priority = priority,
                AssigneeId = dto.AssigneeId,
                CreatedById = CurrentUserId,
                DueDate = dto.DueDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Tasks.Add(task);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Task",
                entityId: task.Id.ToString(),
                action: "TaskCreated",
                changedById: CurrentUserId,
                newValue: System.Text.Json.JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            // Fetch createdby & assignee names
            var creatorName = await _dbContext.Users.Where(u => u.Id == CurrentUserId).Select(u => u.FullName).FirstOrDefaultAsync() ?? "";
            string? assigneeName = null;
            if (task.AssigneeId.HasValue)
            {
                assigneeName = await _dbContext.Users.Where(u => u.Id == task.AssigneeId.Value).Select(u => u.FullName).FirstOrDefaultAsync();
            }

            var resultDto = new TaskDto
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                AssigneeId = task.AssigneeId,
                AssigneeName = assigneeName,
                CreatedById = task.CreatedById,
                CreatedByName = creatorName,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                RowVersion = Convert.ToBase64String(task.RowVersion)
            };

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, resultDto);
        }

        [HttpGet("tasks/{id}")]
        public async Task<IActionResult> GetTask(Guid id)
        {
            var canView = await _permissionService.CanViewTaskAsync(CurrentUserId, id);
            if (!canView)
            {
                return Forbid();
            }

            var task = await _dbContext.Tasks
                .Include(t => t.Assignee)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            var dto = new TaskDto
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                AssigneeId = task.AssigneeId,
                AssigneeName = task.Assignee != null ? task.Assignee.FullName : null,
                CreatedById = task.CreatedById,
                CreatedByName = task.CreatedBy.FullName,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                RowVersion = Convert.ToBase64String(task.RowVersion)
            };

            return Ok(dto);
        }

        [HttpPut("tasks/{id}")]
        public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canEdit = await _permissionService.CanEditTaskAsync(CurrentUserId, id);
            if (!canEdit)
            {
                return Forbid();
            }

            var task = await _dbContext.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            if (!Enum.TryParse<TaskStatus>(dto.Status, out var status))
            {
                return BadRequest(new { message = "Invalid status value." });
            }

            if (!Enum.TryParse<TaskPriority>(dto.Priority, out var priority))
            {
                return BadRequest(new { message = "Invalid priority level." });
            }

            if (dto.AssigneeId.HasValue)
            {
                var isMember = await _dbContext.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == dto.AssigneeId.Value && pm.Status == ProjectMemberStatus.Active);
                if (!isMember)
                {
                    return BadRequest(new { message = "Assignee must be an active member of this project." });
                }
            }

            var oldValue = System.Text.Json.JsonSerializer.Serialize(new
            {
                task.Title,
                task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                task.AssigneeId,
                task.DueDate
            });

            try
            {
                // Business Rules checks
                task.UpdateTitle(dto.Title);
                task.Description = dto.Description;
                task.TransitionTo(status);
                task.Priority = priority;
                task.AssigneeId = dto.AssigneeId;
                task.DueDate = dto.DueDate;
                task.UpdatedAt = DateTime.UtcNow;

                if (_dbContext is DbContext efDbContext)
                {
                    efDbContext.Entry(task).Property("RowVersion").OriginalValue = Convert.FromBase64String(dto.RowVersion);
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "The task was modified by another user. Please reload the task and try again." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            await _auditService.LogAsync(
                entityType: "Task",
                entityId: task.Id.ToString(),
                action: "TaskUpdated",
                changedById: CurrentUserId,
                oldValue: oldValue,
                newValue: System.Text.Json.JsonSerializer.Serialize(new
                {
                    task.Title,
                    task.Description,
                    Status = task.Status.ToString(),
                    Priority = task.Priority.ToString(),
                    task.AssigneeId,
                    task.DueDate
                }),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            // Fetch createdby & assignee names
            var creatorName = await _dbContext.Users.Where(u => u.Id == task.CreatedById).Select(u => u.FullName).FirstOrDefaultAsync() ?? "";
            string? assigneeName = null;
            if (task.AssigneeId.HasValue)
            {
                assigneeName = await _dbContext.Users.Where(u => u.Id == task.AssigneeId.Value).Select(u => u.FullName).FirstOrDefaultAsync();
            }

            return Ok(new TaskDto
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                AssigneeId = task.AssigneeId,
                AssigneeName = assigneeName,
                CreatedById = task.CreatedById,
                CreatedByName = creatorName,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                RowVersion = Convert.ToBase64String(task.RowVersion)
            });
        }

        [HttpPatch("tasks/{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(Guid id, [FromBody] UpdateTaskStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canEditOwn = await _permissionService.CanEditOwnTaskAsync(CurrentUserId, id);
            if (!canEditOwn)
            {
                return Forbid();
            }

            var task = await _dbContext.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            if (!Enum.TryParse<TaskStatus>(dto.Status, out var status))
            {
                return BadRequest(new { message = "Invalid status value." });
            }

            var oldStatus = task.Status.ToString();

            try
            {
                task.TransitionTo(status);
                await _dbContext.SaveChangesAsync();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            await _auditService.LogAsync(
                entityType: "Task",
                entityId: task.Id.ToString(),
                action: "TaskStatusChanged",
                changedById: CurrentUserId,
                oldValue: oldStatus,
                newValue: status.ToString(),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Task status updated successfully.", status = status.ToString() });
        }

        [HttpPatch("tasks/{id}/assignee")]
        public async Task<IActionResult> UpdateTaskAssignee(Guid id, [FromBody] UpdateTaskAssigneeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canEdit = await _permissionService.CanEditTaskAsync(CurrentUserId, id);
            if (!canEdit)
            {
                return Forbid();
            }

            var task = await _dbContext.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            if (dto.AssigneeId.HasValue)
            {
                var isMember = await _dbContext.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == dto.AssigneeId.Value && pm.Status == ProjectMemberStatus.Active);
                if (!isMember)
                {
                    return BadRequest(new { message = "Assignee must be an active member of this project." });
                }
            }

            var oldAssignee = task.AssigneeId?.ToString();
            task.AssigneeId = dto.AssigneeId;
            task.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Task",
                entityId: task.Id.ToString(),
                action: "TaskAssigneeChanged",
                changedById: CurrentUserId,
                oldValue: oldAssignee,
                newValue: dto.AssigneeId?.ToString(),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Task assignee updated successfully.", assigneeId = dto.AssigneeId });
        }

        [HttpDelete("tasks/{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var canDelete = await _permissionService.CanDeleteTaskAsync(CurrentUserId, id);
            if (!canDelete)
            {
                return Forbid();
            }

            var task = await _dbContext.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            task.IsDeleted = true;
            task.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Task",
                entityId: task.Id.ToString(),
                action: "TaskDeleted",
                changedById: CurrentUserId,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Task soft-deleted successfully." });
        }

        [HttpGet("tasks/my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            var tasks = await _dbContext.Tasks
                .Where(t => t.AssigneeId == CurrentUserId)
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    AssigneeId = t.AssigneeId,
                    AssigneeName = t.Assignee != null ? t.Assignee.FullName : null,
                    CreatedById = t.CreatedById,
                    CreatedByName = t.CreatedBy.FullName,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    RowVersion = Convert.ToBase64String(t.RowVersion)
                })
                .ToListAsync();

            return Ok(tasks);
        }
    }
}

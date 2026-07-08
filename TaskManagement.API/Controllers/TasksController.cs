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
using TaskManagement.Domain.Entities;
using TaskManagement.Application.DTOs.Reports;
using TaskManagement.Application.Services;


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
                    ParentTaskId = t.ParentTaskId,
                    ParentTaskTitle = t.ParentTask != null ? t.ParentTask.Title : null,
                    SubtasksCount = t.ChildTasks.Count(c => !c.IsDeleted),
                    CompletedSubtasksCount = t.ChildTasks.Count(c => !c.IsDeleted && c.Status == TaskStatus.Done),
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    RowVersion = Convert.ToBase64String(t.RowVersion)
                })
                .ToListAsync();

            var taskIds = items.Select(t => t.Id).ToList();
            var allDynamicValues = await _dbContext.TaskDynamicFieldValues
                .Where(v => taskIds.Contains(v.TaskId))
                .Include(v => v.DynamicFieldDefinition)
                .ToListAsync();

            foreach (var item in items)
            {
                item.DynamicValues = allDynamicValues
                    .Where(v => v.TaskId == item.Id)
                    .ToDictionary(v => v.DynamicFieldDefinition.FieldKey, v => v.FieldValue ?? string.Empty);
            }

            return Ok(new
            {
                items,
                totalCount,
                page,
                pageSize
            });
        }

        [HttpPost("projects/{projectId}/tasks/advanced")]
        [RequireProjectMembership]
        public async Task<IActionResult> GetProjectTasksAdvanced(
            Guid projectId,
            [FromBody] AdvancedFilterDto filter)
        {
            if (filter == null) return BadRequest(new { message = "Filter body is required." });
            
            var defaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize", 10);
            var maxPageSize = _configuration.GetValue<int>("Pagination:MaxPageSize", 100);

            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1 || filter.PageSize > maxPageSize) filter.PageSize = defaultPageSize;

            var baseQuery = _dbContext.Tasks
                .Where(t => t.ProjectId == projectId);

            var query = baseQuery;

            if (filter.Filter != null)
            {
                var expression = AdvancedQueryBuilder.BuildExpression(filter.Filter);
                query = query.Where(expression);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
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
                    ParentTaskId = t.ParentTaskId,
                    ParentTaskTitle = t.ParentTask != null ? t.ParentTask.Title : null,
                    SubtasksCount = t.ChildTasks.Count(c => !c.IsDeleted),
                    CompletedSubtasksCount = t.ChildTasks.Count(c => !c.IsDeleted && c.Status == TaskStatus.Done),
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    RowVersion = Convert.ToBase64String(t.RowVersion)
                })
                .ToListAsync();

            var taskIds = items.Select(t => t.Id).ToList();
            var allDynamicValues = await _dbContext.TaskDynamicFieldValues
                .Where(v => taskIds.Contains(v.TaskId))
                .Include(v => v.DynamicFieldDefinition)
                .ToListAsync();

            foreach (var item in items)
            {
                item.DynamicValues = allDynamicValues
                    .Where(v => v.TaskId == item.Id)
                    .ToDictionary(v => v.DynamicFieldDefinition.FieldKey, v => v.FieldValue ?? string.Empty);
            }

            return Ok(new
            {
                items,
                totalCount,
                page = filter.Page,
                pageSize = filter.PageSize
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

            if (dto.ParentTaskId.HasValue)
            {
                var parentTask = await _dbContext.Tasks.FindAsync(dto.ParentTaskId.Value);
                if (parentTask == null || parentTask.ProjectId != projectId)
                {
                    return BadRequest(new { message = "Parent task must exist and belong to the same project." });
                }
            }

            // Validate Dynamic Fields
            var definitions = await _dbContext.DynamicFieldDefinitions
                .Where(df => df.ProjectId == projectId && df.IsActive)
                .ToListAsync();

            var errors = new System.Collections.Generic.Dictionary<string, string>();
            var providedValues = dto.DynamicValues ?? new System.Collections.Generic.Dictionary<string, string>();
            foreach (var def in definitions)
            {
                providedValues.TryGetValue(def.FieldKey, out var val);
                if (def.IsRequired && string.IsNullOrWhiteSpace(val))
                {
                    errors[def.FieldKey] = $"{def.FieldName} is required.";
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(val))
                {
                    if (!ValidateValueByType(val, def.FieldType, def.Options, out var valError))
                    {
                        errors[def.FieldKey] = valError;
                    }
                }
            }

            if (errors.Count > 0)
            {
                return BadRequest(new { message = "Validation failed for dynamic fields.", errors });
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
                ParentTaskId = dto.ParentTaskId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Tasks.Add(task);
            await _dbContext.SaveChangesAsync();

            // Save Dynamic Field Values
            if (dto.DynamicValues != null && dto.DynamicValues.Count > 0)
            {
                foreach (var kv in dto.DynamicValues)
                {
                    var def = definitions.FirstOrDefault(d => d.FieldKey.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                    if (def != null && !string.IsNullOrWhiteSpace(kv.Value))
                    {
                        _dbContext.TaskDynamicFieldValues.Add(new TaskDynamicFieldValue
                        {
                            TaskId = task.Id,
                            DynamicFieldId = def.Id,
                            FieldValue = kv.Value
                        });
                    }
                }
                await _dbContext.SaveChangesAsync();
            }

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

            string? parentTaskTitle = null;
            if (task.ParentTaskId.HasValue)
            {
                parentTaskTitle = await _dbContext.Tasks.Where(t => t.Id == task.ParentTaskId.Value).Select(t => t.Title).FirstOrDefaultAsync();
            }

            var savedValues = await _dbContext.TaskDynamicFieldValues
                .Where(v => v.TaskId == task.Id)
                .Include(v => v.DynamicFieldDefinition)
                .ToDictionaryAsync(v => v.DynamicFieldDefinition.FieldKey, v => v.FieldValue ?? string.Empty);

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
                ParentTaskId = task.ParentTaskId,
                ParentTaskTitle = parentTaskTitle,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                RowVersion = Convert.ToBase64String(task.RowVersion),
                DynamicValues = savedValues
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
                .Include(t => t.ParentTask)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            var childTasks = await _dbContext.Tasks
                .Where(t => t.ParentTaskId == id && !t.IsDeleted)
                .Select(t => new SubTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status.ToString()
                })
                .ToListAsync();

            var taskDynamicValues = await _dbContext.TaskDynamicFieldValues
                .Where(v => v.TaskId == id)
                .Include(v => v.DynamicFieldDefinition)
                .ToDictionaryAsync(v => v.DynamicFieldDefinition.FieldKey, v => v.FieldValue ?? string.Empty);

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
                ParentTaskId = task.ParentTaskId,
                ParentTaskTitle = task.ParentTask != null ? task.ParentTask.Title : null,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                RowVersion = Convert.ToBase64String(task.RowVersion),
                SubtasksCount = childTasks.Count,
                CompletedSubtasksCount = childTasks.Count(c => c.Status == "Done"),
                ChildTasks = childTasks,
                DynamicValues = taskDynamicValues
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

            if (dto.ParentTaskId.HasValue)
            {
                if (dto.ParentTaskId.Value == id)
                {
                    return BadRequest(new { message = "A task cannot be its own parent." });
                }

                var parentTask = await _dbContext.Tasks.FindAsync(dto.ParentTaskId.Value);
                if (parentTask == null || parentTask.ProjectId != task.ProjectId)
                {
                    return BadRequest(new { message = "Parent task must exist and belong to the same project." });
                }

                // Check for circular dependency
                var currentAncestorId = parentTask.ParentTaskId;
                while (currentAncestorId.HasValue)
                {
                    if (currentAncestorId.Value == id)
                    {
                        return BadRequest(new { message = "Setting this parent task would create a circular dependency." });
                    }
                    var nextAncestor = await _dbContext.Tasks.FindAsync(currentAncestorId.Value);
                    currentAncestorId = nextAncestor?.ParentTaskId;
                }
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

            // Validate Dynamic Fields
            var definitions = await _dbContext.DynamicFieldDefinitions
                .Where(df => df.ProjectId == task.ProjectId && df.IsActive)
                .ToListAsync();

            var existingValues = await _dbContext.TaskDynamicFieldValues
                .Where(v => v.TaskId == id)
                .Include(v => v.DynamicFieldDefinition)
                .ToDictionaryAsync(v => v.DynamicFieldDefinition.FieldKey, v => v.FieldValue ?? string.Empty);

            var mergedValues = new System.Collections.Generic.Dictionary<string, string>(existingValues, StringComparer.OrdinalIgnoreCase);
            if (dto.DynamicValues != null)
            {
                foreach (var kv in dto.DynamicValues)
                {
                    mergedValues[kv.Key] = kv.Value;
                }
            }

            var errors = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var def in definitions)
            {
                mergedValues.TryGetValue(def.FieldKey, out var val);
                if (def.IsRequired && string.IsNullOrWhiteSpace(val))
                {
                    errors[def.FieldKey] = $"{def.FieldName} is required.";
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(val))
                {
                    if (!ValidateValueByType(val, def.FieldType, def.Options, out var valError))
                    {
                        errors[def.FieldKey] = valError;
                    }
                }
            }

            if (errors.Count > 0)
            {
                return BadRequest(new { message = "Validation failed for dynamic fields.", errors });
            }

            // Serialize old state including old dynamic values
            var oldDynLog = definitions.ToDictionary(
                d => d.FieldKey,
                d => existingValues.TryGetValue(d.FieldKey, out var val) ? val ?? string.Empty : string.Empty
            );

            var oldValue = System.Text.Json.JsonSerializer.Serialize(new
            {
                task.Title,
                task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                task.AssigneeId,
                task.DueDate,
                task.ParentTaskId,
                DynamicValues = oldDynLog
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
                task.ParentTaskId = dto.ParentTaskId;
                task.UpdatedAt = DateTime.UtcNow;

                if (_dbContext is DbContext efDbContext)
                {
                    efDbContext.Entry(task).Property("RowVersion").OriginalValue = Convert.FromBase64String(dto.RowVersion);
                }

                await _dbContext.SaveChangesAsync();

                // Save Dynamic Field Values
                if (dto.DynamicValues != null)
                {
                    var activeDefIds = definitions.Select(d => d.Id).ToList();
                    var valuesToRemove = await _dbContext.TaskDynamicFieldValues
                        .Where(v => v.TaskId == id && activeDefIds.Contains(v.DynamicFieldId))
                        .ToListAsync();
                    _dbContext.TaskDynamicFieldValues.RemoveRange(valuesToRemove);

                    foreach (var kv in dto.DynamicValues)
                    {
                        var def = definitions.FirstOrDefault(d => d.FieldKey.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                        if (def != null && !string.IsNullOrWhiteSpace(kv.Value))
                        {
                            _dbContext.TaskDynamicFieldValues.Add(new TaskDynamicFieldValue
                            {
                                TaskId = id,
                                DynamicFieldId = def.Id,
                                FieldValue = kv.Value
                            });
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                }
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

            var newDynLog = definitions.ToDictionary(
                d => d.FieldKey,
                d => mergedValues.TryGetValue(d.FieldKey, out var val) ? val ?? string.Empty : string.Empty
            );

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
                    task.DueDate,
                    task.ParentTaskId,
                    DynamicValues = newDynLog
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

            string? parentTaskTitleUpdated = null;
            if (task.ParentTaskId.HasValue)
            {
                parentTaskTitleUpdated = await _dbContext.Tasks.Where(t => t.Id == task.ParentTaskId.Value).Select(t => t.Title).FirstOrDefaultAsync();
            }

            var childTasksUpdated = await _dbContext.Tasks
                .Where(t => t.ParentTaskId == id && !t.IsDeleted)
                .Select(t => new SubTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status.ToString()
                })
                .ToListAsync();

            var updatedDynamicValues = await _dbContext.TaskDynamicFieldValues
                .Where(v => v.TaskId == id)
                .Include(v => v.DynamicFieldDefinition)
                .ToDictionaryAsync(v => v.DynamicFieldDefinition.FieldKey, v => v.FieldValue ?? string.Empty);

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
                ParentTaskId = task.ParentTaskId,
                ParentTaskTitle = parentTaskTitleUpdated,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                RowVersion = Convert.ToBase64String(task.RowVersion),
                ChildTasks = childTasksUpdated,
                DynamicValues = updatedDynamicValues
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

            var hasUnfinishedChildren = await _dbContext.Tasks
                .AnyAsync(t => t.ParentTaskId == id && !t.IsDeleted && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);

            if (hasUnfinishedChildren)
            {
                return BadRequest(new { message = "Cannot delete task because it has child tasks that are not Done or Cancelled." });
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

        [HttpGet("tasks/{id}/children")]
        public async Task<IActionResult> GetTaskChildren(Guid id)
        {
            var canView = await _permissionService.CanViewTaskAsync(CurrentUserId, id);
            if (!canView)
            {
                return Forbid();
            }

            var task = await _dbContext.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            var children = await _dbContext.Tasks
                .Where(t => t.ParentTaskId == id && !t.IsDeleted)
                .Select(t => new SubTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status.ToString()
                })
                .ToListAsync();

            return Ok(children);
        }

        [HttpPatch("tasks/{id}/parent")]
        public async Task<IActionResult> SetParentTask(Guid id, [FromBody] SetParentTaskDto dto)
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

            if (dto.ParentTaskId == id)
            {
                return BadRequest(new { message = "A task cannot be its own parent." });
            }

            var parentTask = await _dbContext.Tasks.FindAsync(dto.ParentTaskId);
            if (parentTask == null || parentTask.IsDeleted || parentTask.ProjectId != task.ProjectId)
            {
                return BadRequest(new { message = "Parent task must exist, not be deleted, and belong to the same project." });
            }

            // Check for circular dependency
            var currentAncestorId = parentTask.ParentTaskId;
            while (currentAncestorId.HasValue)
            {
                if (currentAncestorId.Value == id)
                {
                    return BadRequest(new { message = "Setting this parent task would create a circular dependency." });
                }
                var nextAncestor = await _dbContext.Tasks.FindAsync(currentAncestorId.Value);
                currentAncestorId = nextAncestor?.ParentTaskId;
            }

            var oldValue = task.ParentTaskId?.ToString();
            task.ParentTaskId = dto.ParentTaskId;
            task.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Task",
                entityId: task.Id.ToString(),
                action: "TaskParentChanged",
                changedById: CurrentUserId,
                oldValue: oldValue,
                newValue: dto.ParentTaskId.ToString(),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Parent task updated successfully." });
        }

        [HttpPatch("tasks/{id}/remove-parent")]
        public async Task<IActionResult> RemoveParentTask(Guid id)
        {
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

            if (!task.ParentTaskId.HasValue)
            {
                return BadRequest(new { message = "Task does not have a parent task." });
            }

            var oldValue = task.ParentTaskId.Value.ToString();
            task.ParentTaskId = null;
            task.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Task",
                entityId: task.Id.ToString(),
                action: "TaskParentRemoved",
                changedById: CurrentUserId,
                oldValue: oldValue,
                newValue: null,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Parent task removed successfully." });
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
                    ParentTaskId = t.ParentTaskId,
                    ParentTaskTitle = t.ParentTask != null ? t.ParentTask.Title : null,
                    SubtasksCount = t.ChildTasks.Count(c => !c.IsDeleted),
                    CompletedSubtasksCount = t.ChildTasks.Count(c => !c.IsDeleted && c.Status == TaskStatus.Done),
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    RowVersion = Convert.ToBase64String(t.RowVersion)
                })
                .ToListAsync();

            return Ok(tasks);
        }
        private static bool ValidateValueByType(string value, DynamicFieldType type, System.Collections.Generic.List<string> options, out string error)
        {
            error = string.Empty;

            switch (type)
            {
                case DynamicFieldType.Number:
                    if (!double.TryParse(value, out _))
                    {
                        error = "Value must be a valid number.";
                        return false;
                    }
                    break;

                case DynamicFieldType.Date:
                    if (!DateTime.TryParse(value, out _))
                    {
                        error = "Value must be a valid date.";
                        return false;
                    }
                    break;

                case DynamicFieldType.Boolean:
                    var lower = value.ToLower();
                    if (lower != "true" && lower != "false" && lower != "1" && lower != "0")
                    {
                        error = "Value must be a valid boolean (true or false).";
                        return false;
                    }
                    break;

                case DynamicFieldType.Select:
                    if (!options.Any(opt => opt.Equals(value, StringComparison.OrdinalIgnoreCase)))
                    {
                        error = $"Value must be one of the specified options: {string.Join(", ", options)}.";
                        return false;
                    }
                    break;

                case DynamicFieldType.MultiSelect:
                    try
                    {
                        System.Collections.Generic.List<string>? selectedOptions = null;
                        if (value.TrimStart().StartsWith("["))
                        {
                            selectedOptions = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(value);
                        }
                        else
                        {
                            selectedOptions = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToList();
                        }

                        if (selectedOptions == null || selectedOptions.Count == 0)
                        {
                            error = "Value must contain at least one option.";
                            return false;
                        }

                        foreach (var opt in selectedOptions)
                        {
                            if (!options.Any(o => o.Equals(opt, StringComparison.OrdinalIgnoreCase)))
                            {
                                error = $"Option '{opt}' is not a valid choice. Allowed options: {string.Join(", ", options)}.";
                                return false;
                            }
                        }
                    }
                    catch
                    {
                        error = "MultiSelect value must be a valid JSON array of options or comma-separated list.";
                        return false;
                    }
                    break;
            }

            return true;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.API.Filters;
using TaskManagement.Application.DTOs.DynamicFields;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class DynamicFieldsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public DynamicFieldsController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        [HttpGet("projects/{projectId}/dynamic-fields")]
        [RequireProjectMembership]
        public async Task<IActionResult> GetProjectDynamicFields(Guid projectId)
        {
            var fields = await _dbContext.DynamicFieldDefinitions
                .Where(df => df.ProjectId == projectId)
                .OrderBy(df => df.DisplayOrder)
                .Select(df => new DynamicFieldDefinitionDto
                {
                    Id = df.Id,
                    ProjectId = df.ProjectId,
                    FieldName = df.FieldName,
                    FieldKey = df.FieldKey,
                    FieldType = df.FieldType.ToString(),
                    IsRequired = df.IsRequired,
                    Options = df.Options,
                    DefaultValue = df.DefaultValue,
                    DisplayOrder = df.DisplayOrder,
                    IsActive = df.IsActive,
                    CreatedAt = df.CreatedAt,
                    UpdatedAt = df.UpdatedAt
                })
                .ToListAsync();

            return Ok(fields);
        }

        [HttpPost("projects/{projectId}/dynamic-fields")]
        public async Task<IActionResult> CreateDynamicField(Guid projectId, [FromBody] CreateDynamicFieldDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId && p.Status != ProjectStatus.Deleted);
            if (!projectExists) return NotFound(new { message = "Project not found." });

            var isAuthorized = await _permissionService.CanEditProjectAsync(CurrentUserId, projectId);
            if (!isAuthorized) return Forbid();

            if (!Enum.TryParse<DynamicFieldType>(dto.FieldType, true, out var fieldType))
            {
                return BadRequest(new { message = $"Invalid FieldType '{dto.FieldType}'. Supported types are: Text, Number, Date, Boolean, Select, MultiSelect." });
            }

            // Key uniqueness within project
            var keyExists = await _dbContext.DynamicFieldDefinitions
                .AnyAsync(df => df.ProjectId == projectId && df.FieldKey.ToLower() == dto.FieldKey.ToLower());
            if (keyExists)
            {
                return BadRequest(new { message = $"A dynamic field with key '{dto.FieldKey}' already exists in this project." });
            }

            // Validation for Select/MultiSelect options
            if ((fieldType == DynamicFieldType.Select || fieldType == DynamicFieldType.MultiSelect) && (dto.Options == null || dto.Options.Count == 0))
            {
                return BadRequest(new { message = "Options are required for Select and MultiSelect field types." });
            }

            // Validate DefaultValue based on type
            var options = dto.Options ?? new List<string>();
            if (!string.IsNullOrEmpty(dto.DefaultValue) && !ValidateValueByType(dto.DefaultValue, fieldType, options, out string validationError))
            {
                return BadRequest(new { message = $"Default value validation failed: {validationError}" });
            }

            var field = new DynamicFieldDefinition
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                FieldName = dto.FieldName,
                FieldKey = dto.FieldKey,
                FieldType = fieldType,
                IsRequired = dto.IsRequired,
                Options = options,
                DefaultValue = dto.DefaultValue,
                DisplayOrder = dto.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.DynamicFieldDefinitions.Add(field);
            await _dbContext.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                entityType: "DynamicField",
                entityId: field.Id.ToString(),
                action: "DynamicFieldCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return CreatedAtAction(nameof(GetProjectDynamicFields), new { projectId }, new DynamicFieldDefinitionDto
            {
                Id = field.Id,
                ProjectId = field.ProjectId,
                FieldName = field.FieldName,
                FieldKey = field.FieldKey,
                FieldType = field.FieldType.ToString(),
                IsRequired = field.IsRequired,
                Options = field.Options,
                DefaultValue = field.DefaultValue,
                DisplayOrder = field.DisplayOrder,
                IsActive = field.IsActive,
                CreatedAt = field.CreatedAt,
                UpdatedAt = field.UpdatedAt
            });
        }

        [HttpPut("dynamic-fields/{fieldId}")]
        public async Task<IActionResult> UpdateDynamicField(Guid fieldId, [FromBody] UpdateDynamicFieldDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var field = await _dbContext.DynamicFieldDefinitions.FindAsync(fieldId);
            if (field == null) return NotFound(new { message = "Dynamic field definition not found." });

            var isAuthorized = await _permissionService.CanEditProjectAsync(CurrentUserId, field.ProjectId);
            if (!isAuthorized) return Forbid();

            var oldValueJson = JsonSerializer.Serialize(new
            {
                field.FieldName,
                field.IsRequired,
                field.Options,
                field.DefaultValue,
                field.DisplayOrder,
                field.IsActive
            });

            // If Select/MultiSelect, validate options
            if ((field.FieldType == DynamicFieldType.Select || field.FieldType == DynamicFieldType.MultiSelect) && (dto.Options == null || dto.Options.Count == 0))
            {
                return BadRequest(new { message = "Options are required for Select and MultiSelect field types." });
            }

            var options = dto.Options ?? new List<string>();
            if (!string.IsNullOrEmpty(dto.DefaultValue) && !ValidateValueByType(dto.DefaultValue, field.FieldType, options, out string validationError))
            {
                return BadRequest(new { message = $"Default value validation failed: {validationError}" });
            }

            field.FieldName = dto.FieldName;
            field.IsRequired = dto.IsRequired;
            field.Options = options;
            field.DefaultValue = dto.DefaultValue;
            field.DisplayOrder = dto.DisplayOrder;
            field.IsActive = dto.IsActive;
            field.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                entityType: "DynamicField",
                entityId: field.Id.ToString(),
                action: "DynamicFieldUpdated",
                changedById: CurrentUserId,
                oldValue: oldValueJson,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new DynamicFieldDefinitionDto
            {
                Id = field.Id,
                ProjectId = field.ProjectId,
                FieldName = field.FieldName,
                FieldKey = field.FieldKey,
                FieldType = field.FieldType.ToString(),
                IsRequired = field.IsRequired,
                Options = field.Options,
                DefaultValue = field.DefaultValue,
                DisplayOrder = field.DisplayOrder,
                IsActive = field.IsActive,
                CreatedAt = field.CreatedAt,
                UpdatedAt = field.UpdatedAt
            });
        }

        [HttpDelete("dynamic-fields/{fieldId}")]
        public async Task<IActionResult> DeleteDynamicField(Guid fieldId)
        {
            var field = await _dbContext.DynamicFieldDefinitions.FindAsync(fieldId);
            if (field == null) return NotFound(new { message = "Dynamic field definition not found." });

            var isAuthorized = await _permissionService.CanEditProjectAsync(CurrentUserId, field.ProjectId);
            if (!isAuthorized) return Forbid();

            var oldValueJson = JsonSerializer.Serialize(new
            {
                field.Id,
                field.ProjectId,
                field.FieldName,
                field.FieldKey,
                field.FieldType,
                field.Options
            });

            _dbContext.DynamicFieldDefinitions.Remove(field);
            await _dbContext.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                entityType: "DynamicField",
                entityId: fieldId.ToString(),
                action: "DynamicFieldDeleted",
                changedById: CurrentUserId,
                oldValue: oldValueJson,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        [HttpGet("tasks/{taskId}/dynamic-values")]
        public async Task<IActionResult> GetTaskDynamicValues(Guid taskId)
        {
            var canView = await _permissionService.CanViewTaskAsync(CurrentUserId, taskId);
            if (!canView) return Forbid();

            var values = await _dbContext.TaskDynamicFieldValues
                .Where(v => v.TaskId == taskId)
                .Include(v => v.DynamicFieldDefinition)
                .ToDictionaryAsync(v => v.DynamicFieldDefinition.FieldKey, v => v.FieldValue ?? string.Empty);

            return Ok(values);
        }

        [HttpPut("tasks/{taskId}/dynamic-values")]
        public async Task<IActionResult> UpdateTaskDynamicValues(Guid taskId, [FromBody] Dictionary<string, string> values)
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null) return NotFound(new { message = "Task not found." });

            var canEdit = await _permissionService.CanEditOwnTaskAsync(CurrentUserId, taskId);
            if (!canEdit) return Forbid();

            // Fetch dynamic field definitions for project
            var definitions = await _dbContext.DynamicFieldDefinitions
                .Where(df => df.ProjectId == task.ProjectId && df.IsActive)
                .ToListAsync();

            var existingValues = await _dbContext.TaskDynamicFieldValues
                .Where(v => v.TaskId == taskId)
                .ToListAsync();

            var oldValueDict = existingValues
                .ToDictionary(v => v.DynamicFieldId, v => v.FieldValue);

            var errors = new Dictionary<string, string>();
            var newValuesToSave = new List<TaskDynamicFieldValue>();

            foreach (var definition in definitions)
            {
                values.TryGetValue(definition.FieldKey, out var providedValue);

                // Required check
                if (definition.IsRequired && string.IsNullOrWhiteSpace(providedValue))
                {
                    errors[definition.FieldKey] = $"{definition.FieldName} is required.";
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(providedValue))
                {
                    // Validate structure/type
                    if (!ValidateValueByType(providedValue, definition.FieldType, definition.Options, out string validationError))
                    {
                        errors[definition.FieldKey] = validationError;
                        continue;
                    }

                    newValuesToSave.Add(new TaskDynamicFieldValue
                    {
                        TaskId = taskId,
                        DynamicFieldId = definition.Id,
                        FieldValue = providedValue
                    });
                }
            }

            if (errors.Count > 0)
            {
                return BadRequest(new { message = "Validation failed for dynamic fields.", errors });
            }

            // Save values
            // Remove existing values for active definitions first
            var activeDefIds = definitions.Select(d => d.Id).ToList();
            var valuesToRemove = existingValues.Where(v => activeDefIds.Contains(v.DynamicFieldId));
            _dbContext.TaskDynamicFieldValues.RemoveRange(valuesToRemove);

            // Add new values
            _dbContext.TaskDynamicFieldValues.AddRange(newValuesToSave);
            await _dbContext.SaveChangesAsync();

            // Build old/new values dictionary for audit log
            var oldLogDict = definitions.ToDictionary(
                d => d.FieldKey,
                d => oldValueDict.TryGetValue(d.Id, out var val) ? val ?? string.Empty : string.Empty
            );

            var newLogDict = definitions.ToDictionary(
                d => d.FieldKey,
                d => values.TryGetValue(d.FieldKey, out var val) ? val ?? string.Empty : string.Empty
            );

            await _auditService.LogAsync(
                entityType: "Task",
                entityId: taskId.ToString(),
                action: "TaskDynamicValuesUpdated",
                changedById: CurrentUserId,
                oldValue: JsonSerializer.Serialize(oldLogDict),
                newValue: JsonSerializer.Serialize(newLogDict),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(newLogDict);
        }

        private static bool ValidateValueByType(string value, DynamicFieldType type, List<string> options, out string error)
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
                        // Check if it's a valid JSON array or a comma-separated list
                        List<string>? selectedOptions = null;
                        if (value.TrimStart().StartsWith("["))
                        {
                            selectedOptions = JsonSerializer.Deserialize<List<string>>(value);
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

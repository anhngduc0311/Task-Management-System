using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs.Reports;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Task = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IAppDbContext _dbContext;

        public ReportService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private async Task<IQueryable<Task>> GetAuthorizedTasksQueryAsync(Guid userId)
        {
            var isAdmin = await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == 1);
            var query = _dbContext.Tasks.Where(t => !t.IsDeleted);

            if (!isAdmin)
            {
                query = query.Where(t =>
                    t.Project.OwnerId == userId ||
                    _dbContext.ProjectMembers.Any(pm => pm.ProjectId == t.ProjectId && pm.UserId == userId && pm.Status == ProjectMemberStatus.Active)
                );
            }

            return query;
        }

        private IQueryable<Task> ApplyFilters(IQueryable<Task> query, ReportFilterDto filter)
        {
            if (filter == null) return query;

            if (filter.ProjectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == filter.ProjectId.Value);
            }

            if (filter.AssigneeId.HasValue)
            {
                query = query.Where(t => t.AssigneeId == filter.AssigneeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<TaskStatus>(filter.Status, true, out var statusVal))
            {
                query = query.Where(t => t.Status == statusVal);
            }

            if (!string.IsNullOrWhiteSpace(filter.Priority) && Enum.TryParse<TaskPriority>(filter.Priority, true, out var priorityVal))
            {
                query = query.Where(t => t.Priority == priorityVal);
            }

            // Generic Date Range: filters by CreatedAt if specified
            if (filter.DateFrom.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= filter.DateFrom.Value);
            }
            if (filter.DateTo.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= filter.DateTo.Value);
            }

            // Specific Date Ranges
            if (filter.CreatedAtFrom.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= filter.CreatedAtFrom.Value);
            }
            if (filter.CreatedAtTo.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= filter.CreatedAtTo.Value);
            }

            if (filter.DueDateFrom.HasValue)
            {
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= filter.DueDateFrom.Value);
            }
            if (filter.DueDateTo.HasValue)
            {
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value <= filter.DueDateTo.Value);
            }

            if (filter.CompletedAtFrom.HasValue)
            {
                query = query.Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= filter.CompletedAtFrom.Value);
            }
            if (filter.CompletedAtTo.HasValue)
            {
                query = query.Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value <= filter.CompletedAtTo.Value);
            }

            // Dynamic Fields filter
            if (filter.DynamicFields != null && filter.DynamicFields.Count > 0)
            {
                foreach (var df in filter.DynamicFields)
                {
                    var key = df.Key;
                    var val = df.Value;
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        query = query.Where(t => t.DynamicFieldValues.Any(dfv =>
                            dfv.DynamicFieldDefinition.FieldKey.ToLower() == key.ToLower() &&
                            dfv.FieldValue != null && dfv.FieldValue.ToLower().Contains(val.ToLower())));
                    }
                }
            }

            return query;
        }

        public async Task<WorkSummaryReportDto> GetWorkSummaryReportAsync(Guid userId, ReportFilterDto filter)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = ApplyFilters(baseQuery, filter);

            var totalTasks = await query.CountAsync();
            if (totalTasks == 0)
            {
                return new WorkSummaryReportDto();
            }

            var now = DateTime.UtcNow;

            var stats = await query
                .Select(t => new
                {
                    t.Status,
                    t.Priority,
                    IsOverdue = t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled,
                    IsSoonDue = t.DueDate.HasValue && t.DueDate.Value >= now && t.DueDate.Value <= now.AddDays(3) && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled,
                    IsCompletedOnTime = t.Status == TaskStatus.Done && (t.CompletedAt <= t.DueDate || !t.DueDate.HasValue),
                    IsCompletedLate = t.Status == TaskStatus.Done && t.DueDate.HasValue && t.CompletedAt > t.DueDate.Value
                })
                .ToListAsync();

            var statusCounts = stats
                .GroupBy(s => s.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var priorityCounts = stats
                .GroupBy(s => s.Priority.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var completedCount = statusCounts.TryGetValue(TaskStatus.Done.ToString(), out var done) ? done : 0;
            var overdueCount = stats.Count(s => s.IsOverdue);
            var soonDueCount = stats.Count(s => s.IsSoonDue);
            var completedOnTimeCount = stats.Count(s => s.IsCompletedOnTime);
            var completedLateCount = stats.Count(s => s.IsCompletedLate);

            return new WorkSummaryReportDto
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedCount,
                OverdueTasks = overdueCount,
                SoonDueTasks = soonDueCount,
                CompletedOnTime = completedOnTimeCount,
                CompletedLate = completedLateCount,
                CompletionRate = Math.Round((double)completedCount / totalTasks * 100, 2),
                StatusCounts = statusCounts,
                PriorityCounts = priorityCounts
            };
        }

        public async Task<List<StatusReportDto>> GetStatusReportAsync(Guid userId, ReportFilterDto filter)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = ApplyFilters(baseQuery, filter);

            return await query
                .GroupBy(t => t.Status)
                .Select(g => new StatusReportDto
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();
        }

        public async Task<List<PriorityReportDto>> GetPriorityReportAsync(Guid userId, ReportFilterDto filter)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = ApplyFilters(baseQuery, filter);

            return await query
                .GroupBy(t => t.Priority)
                .Select(g => new PriorityReportDto
                {
                    Priority = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();
        }

        public async Task<List<AssigneeReportDto>> GetAssigneeReportAsync(Guid userId, ReportFilterDto filter)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = ApplyFilters(baseQuery, filter);

            var now = DateTime.UtcNow;

            return await query
                .Where(t => t.AssigneeId != null)
                .GroupBy(t => new { t.AssigneeId, t.Assignee!.FullName, t.Assignee.Email })
                .Select(g => new AssigneeReportDto
                {
                    AssigneeId = g.Key.AssigneeId,
                    AssigneeName = g.Key.FullName,
                    AssigneeEmail = g.Key.Email,
                    TaskCount = g.Count(),
                    CompletedCount = g.Count(t => t.Status == TaskStatus.Done),
                    OverdueCount = g.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled)
                })
                .ToListAsync();
        }

        public async Task<List<ProjectReportDto>> GetProjectReportAsync(Guid userId, ReportFilterDto filter)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = ApplyFilters(baseQuery, filter);

            var now = DateTime.UtcNow;

            return await query
                .GroupBy(t => new { t.ProjectId, t.Project.Name })
                .Select(g => new ProjectReportDto
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    TaskCount = g.Count(),
                    CompletedCount = g.Count(t => t.Status == TaskStatus.Done),
                    OverdueCount = g.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled)
                })
                .ToListAsync();
        }

        private async Task<ReportPaginatedList<TaskReportDto>> GetPaginatedTasksAsync(IQueryable<Task> query, int page, int pageSize)
        {
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TaskReportDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project.Name,
                    Title = t.Title,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    AssigneeId = t.AssigneeId,
                    AssigneeName = t.Assignee != null ? t.Assignee.FullName : null,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            var taskIds = items.Select(i => i.Id).ToList();
            var dynamicValues = await _dbContext.TaskDynamicFieldValues
                .Where(v => taskIds.Contains(v.TaskId))
                .Include(v => v.DynamicFieldDefinition)
                .ToListAsync();

            foreach (var item in items)
            {
                item.DynamicValues = dynamicValues
                    .Where(v => v.TaskId == item.Id)
                    .ToDictionary(v => v.DynamicFieldDefinition.FieldKey, v => v.FieldValue ?? string.Empty);
            }

            return new ReportPaginatedList<TaskReportDto>(items, totalCount, page, pageSize);
        }

        public async Task<ReportPaginatedList<TaskReportDto>> GetOverdueTasksReportAsync(Guid userId, ReportFilterDto filter, int page, int pageSize)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = ApplyFilters(baseQuery, filter);
            var now = DateTime.UtcNow;

            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);
            return await GetPaginatedTasksAsync(query, page, pageSize);
        }

        public async Task<ReportPaginatedList<TaskReportDto>> GetCompletedTasksReportAsync(Guid userId, ReportFilterDto filter, int page, int pageSize)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = ApplyFilters(baseQuery, filter);

            query = query.Where(t => t.Status == TaskStatus.Done);
            return await GetPaginatedTasksAsync(query, page, pageSize);
        }

        public async Task<ReportPaginatedList<TaskReportDto>> GetUncompletedTasksReportAsync(Guid userId, ReportFilterDto filter, int page, int pageSize)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = ApplyFilters(baseQuery, filter);

            query = query.Where(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);
            return await GetPaginatedTasksAsync(query, page, pageSize);
        }

        public async Task<ReportPaginatedList<TaskReportDto>> GetAdvancedTasksReportAsync(Guid userId, AdvancedFilterDto filter)
        {
            var baseQuery = await GetAuthorizedTasksQueryAsync(userId);
            var query = baseQuery;

            if (filter.Filter != null)
            {
                var expression = AdvancedQueryBuilder.BuildExpression(filter.Filter);
                query = query.Where(expression);
            }

            return await GetPaginatedTasksAsync(query, filter.Page, filter.PageSize);
        }
    }

    public static class AdvancedQueryBuilder
    {
        public static Expression<Func<Task, bool>> BuildExpression(FilterGroupDto group)
        {
            var parameter = Expression.Parameter(typeof(Task), "t");
            var expression = BuildExpressionForGroup(group, parameter);
            if (expression == null)
            {
                return t => true;
            }
            return Expression.Lambda<Func<Task, bool>>(expression, parameter);
        }

        private static Expression? BuildExpressionForGroup(FilterGroupDto group, ParameterExpression parameter)
        {
            var subExpressions = new List<Expression>();

            if (group.Rules != null)
            {
                foreach (var rule in group.Rules)
                {
                    var ruleExpr = BuildExpressionForRule(rule, parameter);
                    if (ruleExpr != null) subExpressions.Add(ruleExpr);
                }
            }

            if (group.Groups != null)
            {
                foreach (var subGroup in group.Groups)
                {
                    var subGroupExpr = BuildExpressionForGroup(subGroup, parameter);
                    if (subGroupExpr != null) subExpressions.Add(subGroupExpr);
                }
            }

            if (subExpressions.Count == 0) return null;

            Expression combined = subExpressions[0];
            for (int i = 1; i < subExpressions.Count; i++)
            {
                if (group.Operator.Equals("OR", StringComparison.OrdinalIgnoreCase))
                {
                    combined = Expression.OrElse(combined, subExpressions[i]);
                }
                else
                {
                    combined = Expression.AndAlso(combined, subExpressions[i]);
                }
            }

            return combined;
        }

        private static Expression? BuildExpressionForRule(FilterRuleDto rule, ParameterExpression parameter)
        {
            if (string.IsNullOrWhiteSpace(rule.Field)) return null;

            var propInfo = typeof(Task).GetProperty(rule.Field);
            var isStandardField = propInfo != null;

            if (isStandardField)
            {
                var property = Expression.Property(parameter, rule.Field);
                var memberType = property.Type;

                object? parsedValue = null;
                object? parsedValueTo = null;

                try
                {
                    if (memberType == typeof(Guid) || memberType == typeof(Guid?))
                    {
                        if (Guid.TryParse(rule.Value, out var guidVal)) parsedValue = guidVal;
                    }
                    else if (memberType == typeof(DateTime) || memberType == typeof(DateTime?))
                    {
                        if (DateTime.TryParse(rule.Value, out var dtVal)) parsedValue = dtVal.ToUniversalTime();
                        if (!string.IsNullOrEmpty(rule.ValueTo) && DateTime.TryParse(rule.ValueTo, out var dtValTo))
                        {
                            parsedValueTo = dtValTo.ToUniversalTime();
                        }
                    }
                    else if (memberType == typeof(TaskStatus) || memberType == typeof(TaskStatus?))
                    {
                        if (Enum.TryParse<TaskStatus>(rule.Value, true, out var statusVal)) parsedValue = statusVal;
                    }
                    else if (memberType == typeof(TaskPriority) || memberType == typeof(TaskPriority?))
                    {
                        if (Enum.TryParse<TaskPriority>(rule.Value, true, out var priorityVal)) parsedValue = priorityVal;
                    }
                    else if (memberType == typeof(string))
                    {
                        parsedValue = rule.Value;
                    }
                    else if (memberType == typeof(bool) || memberType == typeof(bool?))
                    {
                        if (bool.TryParse(rule.Value, out var boolVal)) parsedValue = boolVal;
                    }
                }
                catch
                {
                    return null;
                }

                if (parsedValue == null && rule.Value != null && memberType != typeof(string)) return null;

                Expression rightValue = Expression.Constant(parsedValue, memberType);

                switch (rule.Operator.ToLower())
                {
                    case "equals":
                    case "eq":
                        return Expression.Equal(property, rightValue);
                    case "notequals":
                    case "neq":
                        return Expression.NotEqual(property, rightValue);
                    case "contains":
                        if (memberType == typeof(string))
                        {
                            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                            if (containsMethod != null)
                            {
                                var valueNotNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
                                var containsCall = Expression.Call(property, containsMethod, rightValue);
                                return Expression.AndAlso(valueNotNull, containsCall);
                            }
                        }
                        return Expression.Equal(property, rightValue);
                    case "greaterthan":
                    case "gt":
                        return Expression.GreaterThan(property, rightValue);
                    case "lessthan":
                    case "lt":
                        return Expression.LessThan(property, rightValue);
                    case "between":
                        if (parsedValueTo != null)
                        {
                            Expression rightValueTo = Expression.Constant(parsedValueTo, memberType);
                            var ge = Expression.GreaterThanOrEqual(property, rightValue);
                            var le = Expression.LessThanOrEqual(property, rightValueTo);
                            return Expression.AndAlso(ge, le);
                        }
                        return Expression.GreaterThanOrEqual(property, rightValue);
                    default:
                        return Expression.Equal(property, rightValue);
                }
            }
            else
            {
                // Dynamic Field
                return BuildDynamicFieldExpression(rule.Field, rule.Operator, rule.Value, rule.ValueTo, parameter);
            }
        }

        private static Expression BuildDynamicFieldExpression(string fieldKey, string op, string value, string? valueTo, ParameterExpression taskParameter)
        {
            var dynamicFieldValuesProp = Expression.Property(taskParameter, "DynamicFieldValues");
            var dfvType = typeof(TaskDynamicFieldValue);
            var dfvParam = Expression.Parameter(dfvType, "dfv");

            var definitionProp = Expression.Property(dfvParam, "DynamicFieldDefinition");
            var fieldKeyProp = Expression.Property(definitionProp, "FieldKey");

            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
            var fieldKeyLowerCall = Expression.Call(fieldKeyProp, toLowerMethod);

            var targetKeyConst = Expression.Constant(fieldKey.ToLower(), typeof(string));
            var keyEqualExpr = Expression.Equal(fieldKeyLowerCall, targetKeyConst);

            var fieldValueProp = Expression.Property(dfvParam, "FieldValue");
            var fieldValueNotNull = Expression.NotEqual(fieldValueProp, Expression.Constant(null, typeof(string)));
            var fieldValueLowerCall = Expression.Call(fieldValueProp, toLowerMethod);

            Expression valueCondition;
            var valLower = value.ToLower();

            switch (op.ToLower())
            {
                case "equals":
                case "eq":
                    valueCondition = Expression.Equal(fieldValueLowerCall, Expression.Constant(valLower));
                    break;
                case "notequals":
                case "neq":
                    valueCondition = Expression.NotEqual(fieldValueLowerCall, Expression.Constant(valLower));
                    break;
                case "contains":
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                    valueCondition = Expression.Call(fieldValueLowerCall, containsMethod, Expression.Constant(valLower));
                    break;
                default:
                    valueCondition = Expression.Equal(fieldValueLowerCall, Expression.Constant(valLower));
                    break;
            }

            var fieldValueCondition = Expression.AndAlso(fieldValueNotNull, valueCondition);
            var lambdaBody = Expression.AndAlso(keyEqualExpr, fieldValueCondition);

            var lambdaType = typeof(Func<,>).MakeGenericType(dfvType, typeof(bool));
            var anyLambda = Expression.Lambda(lambdaType, lambdaBody, dfvParam);

            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(dfvType);

            return Expression.Call(anyMethod, dynamicFieldValuesProp, anyLambda);
        }
    }
}

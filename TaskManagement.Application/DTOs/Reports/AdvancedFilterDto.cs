using System.Collections.Generic;

namespace TaskManagement.Application.DTOs.Reports
{
    public class AdvancedFilterDto
    {
        public FilterGroupDto? Filter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class FilterGroupDto
    {
        public string Operator { get; set; } = "AND"; // "AND" or "OR"
        public List<FilterRuleDto>? Rules { get; set; }
        public List<FilterGroupDto>? Groups { get; set; }
    }

    public class FilterRuleDto
    {
        public string Field { get; set; } = string.Empty; // e.g. "ProjectId", "AssigneeId", "Status", "Priority", "CreatedAt", "DueDate", "CompletedAt", or dynamic field key
        public string Operator { get; set; } = "Equals"; // "Equals", "NotEquals", "Contains", "GreaterThan", "LessThan", "Between"
        public string Value { get; set; } = string.Empty;
        public string? ValueTo { get; set; } // for "Between"
    }
}

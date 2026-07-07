using System;
using System.Collections.Generic;

namespace TaskManagement.Application.DTOs.Reports
{
    public class WorkSummaryReportDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int SoonDueTasks { get; set; }
        public int CompletedOnTime { get; set; }
        public int CompletedLate { get; set; }
        public double CompletionRate { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PriorityCounts { get; set; } = new Dictionary<string, int>();
    }

    public class StatusReportDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PriorityReportDto
    {
        public string Priority { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AssigneeReportDto
    {
        public Guid? AssigneeId { get; set; }
        public string AssigneeName { get; set; } = string.Empty;
        public string AssigneeEmail { get; set; } = string.Empty;
        public int TaskCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
    }

    public class ProjectReportDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TaskCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
    }

    public class TaskReportDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public Guid? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, string> DynamicValues { get; set; } = new Dictionary<string, string>();
    }

    public class ReportPaginatedList<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public ReportPaginatedList() { }
        public ReportPaginatedList(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }
    }
}

using System;
using System.Collections.Generic;

namespace TaskManagement.Application.DTOs.Reports
{
    public class ReportFilterDto
    {
        public Guid? ProjectId { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public DateTime? CreatedAtFrom { get; set; }
        public DateTime? CreatedAtTo { get; set; }

        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }

        public DateTime? CompletedAtFrom { get; set; }
        public DateTime? CompletedAtTo { get; set; }

        public Dictionary<string, string>? DynamicFields { get; set; }
    }
}

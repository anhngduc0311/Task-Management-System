using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Reports;

namespace TaskManagement.Application.Interfaces
{
    public interface IReportService
    {
        Task<WorkSummaryReportDto> GetWorkSummaryReportAsync(Guid userId, ReportFilterDto filter);
        Task<List<StatusReportDto>> GetStatusReportAsync(Guid userId, ReportFilterDto filter);
        Task<List<PriorityReportDto>> GetPriorityReportAsync(Guid userId, ReportFilterDto filter);
        Task<List<AssigneeReportDto>> GetAssigneeReportAsync(Guid userId, ReportFilterDto filter);
        Task<List<ProjectReportDto>> GetProjectReportAsync(Guid userId, ReportFilterDto filter);
        Task<ReportPaginatedList<TaskReportDto>> GetOverdueTasksReportAsync(Guid userId, ReportFilterDto filter, int page, int pageSize);
        Task<ReportPaginatedList<TaskReportDto>> GetCompletedTasksReportAsync(Guid userId, ReportFilterDto filter, int page, int pageSize);
        Task<ReportPaginatedList<TaskReportDto>> GetUncompletedTasksReportAsync(Guid userId, ReportFilterDto filter, int page, int pageSize);
        Task<ReportPaginatedList<TaskReportDto>> GetAdvancedTasksReportAsync(Guid userId, AdvancedFilterDto filter);
    }
}

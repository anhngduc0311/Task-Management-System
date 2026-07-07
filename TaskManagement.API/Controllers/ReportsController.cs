using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Reports;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : BaseApiController
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("work-summary")]
        public async Task<IActionResult> GetWorkSummary([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetWorkSummaryReportAsync(CurrentUserId, filter);
            return Ok(result);
        }

        [HttpGet("tasks-by-status")]
        public async Task<IActionResult> GetTasksByStatus([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetStatusReportAsync(CurrentUserId, filter);
            return Ok(result);
        }

        [HttpGet("tasks-by-priority")]
        public async Task<IActionResult> GetTasksByPriority([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetPriorityReportAsync(CurrentUserId, filter);
            return Ok(result);
        }

        [HttpGet("tasks-by-assignee")]
        public async Task<IActionResult> GetTasksByAssignee([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetAssigneeReportAsync(CurrentUserId, filter);
            return Ok(result);
        }

        [HttpGet("tasks-by-project")]
        public async Task<IActionResult> GetTasksByProject([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetProjectReportAsync(CurrentUserId, filter);
            return Ok(result);
        }

        [HttpGet("overdue-tasks")]
        public async Task<IActionResult> GetOverdueTasks([FromQuery] ReportFilterDto filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _reportService.GetOverdueTasksReportAsync(CurrentUserId, filter, page, pageSize);
            return Ok(result);
        }

        [HttpGet("completed-tasks")]
        public async Task<IActionResult> GetCompletedTasks([FromQuery] ReportFilterDto filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _reportService.GetCompletedTasksReportAsync(CurrentUserId, filter, page, pageSize);
            return Ok(result);
        }

        [HttpGet("uncompleted-tasks")]
        public async Task<IActionResult> GetUncompletedTasks([FromQuery] ReportFilterDto filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _reportService.GetUncompletedTasksReportAsync(CurrentUserId, filter, page, pageSize);
            return Ok(result);
        }

        [HttpPost("advanced")]
        public async Task<IActionResult> Advanced([FromBody] AdvancedFilterDto filter)
        {
            if (filter == null) return BadRequest(new { message = "Filter body is required." });
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 10;

            var result = await _reportService.GetAdvancedTasksReportAsync(CurrentUserId, filter);
            return Ok(result);
        }
    }
}

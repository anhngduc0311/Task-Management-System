using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Reports;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using Xunit;
using DomainTask = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using Task = System.Threading.Tasks.Task;

namespace TaskManagement.API.IntegrationTests
{
    public class ReportsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public ReportsControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        private HttpClient GetAuthenticatedClient(User user, string systemRole = "Member")
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var scope = _factory.Services.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var token = tokenService.GenerateAccessToken(user, new[] { systemRole });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(Project project, User pm, User member, User stranger)> SeedDataAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Clean up DB
            db.TaskDynamicFieldValues.RemoveRange(db.TaskDynamicFieldValues);
            db.DynamicFieldDefinitions.RemoveRange(db.DynamicFieldDefinitions);
            db.TaskAttachments.RemoveRange(db.TaskAttachments);
            db.TaskComments.RemoveRange(db.TaskComments);
            db.Tasks.RemoveRange(db.Tasks);
            db.ProjectMembers.RemoveRange(db.ProjectMembers);
            db.Projects.RemoveRange(db.Projects);
            db.Users.RemoveRange(db.Users);
            await db.SaveChangesAsync();

            // Users
            var pm = new User { Id = Guid.NewGuid(), Email = $"pm_{Guid.NewGuid()}@test.com", FullName = "Project Manager", Status = UserStatus.Active };
            var member = new User { Id = Guid.NewGuid(), Email = $"mem_{Guid.NewGuid()}@test.com", FullName = "Regular Member", Status = UserStatus.Active };
            var stranger = new User { Id = Guid.NewGuid(), Email = $"stranger_{Guid.NewGuid()}@test.com", FullName = "Stranger User", Status = UserStatus.Active };

            db.Users.AddRange(pm, member, stranger);
            await db.SaveChangesAsync();

            // Project
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Reports Test Project",
                Description = "A project created for testing reports",
                OwnerId = pm.Id,
                Status = ProjectStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            // Members
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = pm.Id,
                RoleInProject = ProjectMemberRole.ProjectManager,
                Status = ProjectMemberStatus.Active,
                JoinedAt = DateTime.UtcNow.AddDays(-10)
            });

            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = member.Id,
                RoleInProject = ProjectMemberRole.Member,
                Status = ProjectMemberStatus.Active,
                JoinedAt = DateTime.UtcNow.AddDays(-10)
            });

            await db.SaveChangesAsync();

            // Dynamic Field Definition
            var dynField = new DynamicFieldDefinition
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                FieldName = "Department Code",
                FieldKey = "dept_code",
                FieldType = DynamicFieldType.Text,
                IsRequired = false,
                DisplayOrder = 1,
                IsActive = true
            };
            db.DynamicFieldDefinitions.Add(dynField);
            await db.SaveChangesAsync();

            // Tasks
            // 1. Completed Task (on time)
            var t1 = new DomainTask
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = "Completed task",
                Status = TaskStatus.Done,
                Priority = TaskPriority.High,
                AssigneeId = member.Id,
                CreatedById = pm.Id,
                DueDate = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };

            // 2. Overdue Task (not done, in progress)
            var t2 = new DomainTask
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = "Overdue task",
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.Medium,
                AssigneeId = member.Id,
                CreatedById = pm.Id,
                DueDate = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };

            // 3. Normal Task (pending)
            var t3 = new DomainTask
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Title = "Future task",
                Status = TaskStatus.Todo,
                Priority = TaskPriority.Low,
                AssigneeId = pm.Id,
                CreatedById = pm.Id,
                DueDate = DateTime.UtcNow.AddDays(5),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            db.Tasks.AddRange(t1, t2, t3);
            await db.SaveChangesAsync();

            // Dynamic Field value for t1
            db.TaskDynamicFieldValues.Add(new TaskDynamicFieldValue
            {
                TaskId = t1.Id,
                DynamicFieldId = dynField.Id,
                FieldValue = "IT-Dept"
            });
            await db.SaveChangesAsync();

            return (project, pm, member, stranger);
        }

        [Fact]
        public async Task GetWorkSummary_AuthorizedUser_ReturnsCorrectStats()
        {
            // Arrange
            var (project, pm, member, stranger) = await SeedDataAsync();
            var client = GetAuthenticatedClient(member);

            // Act
            var response = await client.GetAsync($"/api/reports/work-summary?projectId={project.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var report = await response.Content.ReadFromJsonAsync<WorkSummaryReportDto>();
            Assert.NotNull(report);
            Assert.Equal(3, report.TotalTasks);
            Assert.Equal(1, report.CompletedTasks);
            Assert.Equal(1, report.OverdueTasks);
            Assert.Equal(33.33, report.CompletionRate);
            Assert.Equal(1, report.CompletedOnTime);
            Assert.Equal(0, report.CompletedLate);
        }

        [Fact]
        public async Task GetWorkSummary_StrangerUser_ReturnsEmptyStats()
        {
            // Arrange
            var (project, pm, member, stranger) = await SeedDataAsync();
            var client = GetAuthenticatedClient(stranger);

            // Act
            var response = await client.GetAsync($"/api/reports/work-summary?projectId={project.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var report = await response.Content.ReadFromJsonAsync<WorkSummaryReportDto>();
            Assert.NotNull(report);
            Assert.Equal(0, report.TotalTasks); // Since stranger cannot access project tasks
        }

        [Fact]
        public async Task GetOverdueTasks_ReturnsPaginatedList()
        {
            // Arrange
            var (project, pm, member, stranger) = await SeedDataAsync();
            var client = GetAuthenticatedClient(pm);

            // Act
            var response = await client.GetAsync($"/api/reports/overdue-tasks?projectId={project.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var report = await response.Content.ReadFromJsonAsync<ReportPaginatedList<TaskReportDto>>();
            Assert.NotNull(report);
            Assert.Equal(1, report.TotalCount);
            Assert.Single(report.Items);
            Assert.Equal("Overdue task", report.Items.First().Title);
        }

        [Fact]
        public async Task POSTAdvanced_FilterByDynamicField_ReturnsCorrectTasks()
        {
            // Arrange
            var (project, pm, member, stranger) = await SeedDataAsync();
            var client = GetAuthenticatedClient(pm);

            var filter = new AdvancedFilterDto
            {
                Filter = new FilterGroupDto
                {
                    Operator = "AND",
                    Rules = new List<FilterRuleDto>
                    {
                        new FilterRuleDto
                        {
                            Field = "dept_code",
                            Operator = "Equals",
                            Value = "IT-Dept"
                        }
                    }
                }
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/reports/advanced", filter);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var report = await response.Content.ReadFromJsonAsync<ReportPaginatedList<TaskReportDto>>();
            Assert.NotNull(report);
            Assert.Equal(1, report.TotalCount);
            Assert.Single(report.Items);
            Assert.Equal("Completed task", report.Items.First().Title);
            Assert.Equal("IT-Dept", report.Items.First().DynamicValues["dept_code"]);
        }
    }
}

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Tasks;
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
    public class TaskCrudIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public TaskCrudIntegrationTests(CustomWebApplicationFactory<Program> factory)
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

            // Clean up DB first
            db.TaskAttachments.RemoveRange(db.TaskAttachments);
            db.TaskComments.RemoveRange(db.TaskComments);
            db.Tasks.RemoveRange(db.Tasks);
            db.ProjectMembers.RemoveRange(db.ProjectMembers);
            db.Projects.RemoveRange(db.Projects);
            db.Users.RemoveRange(db.Users);
            await db.SaveChangesAsync();

            // Create users
            var pm = new User { Id = Guid.NewGuid(), Email = $"pm_{Guid.NewGuid()}@test.com", FullName = "Project Manager", Status = UserStatus.Active };
            var member = new User { Id = Guid.NewGuid(), Email = $"mem_{Guid.NewGuid()}@test.com", FullName = "Regular Member", Status = UserStatus.Active };
            var stranger = new User { Id = Guid.NewGuid(), Email = $"stranger_{Guid.NewGuid()}@test.com", FullName = "Stranger User", Status = UserStatus.Active };

            db.Users.AddRange(pm, member, stranger);
            await db.SaveChangesAsync();

            // Create Project
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Integration Test Project",
                Description = "A project created for testing",
                OwnerId = pm.Id,
                Status = ProjectStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            // Add PM as ProjectManager member
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = pm.Id,
                RoleInProject = ProjectMemberRole.ProjectManager,
                Status = ProjectMemberStatus.Active,
                JoinedAt = DateTime.UtcNow
            });

            // Add Member as Member role
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = member.Id,
                RoleInProject = ProjectMemberRole.Member,
                Status = ProjectMemberStatus.Active,
                JoinedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return (project, pm, member, stranger);
        }

        [Fact]
        public async Task Task_CRUD_Flow_WithPermissions_ShouldWorkCorrectly()
        {
            // 1. Seed data
            var (project, pm, member, stranger) = await SeedDataAsync();

            var pmClient = GetAuthenticatedClient(pm, "ProjectManager");
            var memberClient = GetAuthenticatedClient(member, "Member");
            var strangerClient = GetAuthenticatedClient(stranger, "Member");

            // 2. PM creates a task
            var createTaskDto = new CreateTaskDto
            {
                Title = "New Integration Task",
                Description = "Task details",
                Priority = "High",
                AssigneeId = member.Id,
                DueDate = DateTime.UtcNow.AddDays(3)
            };

            var createResponse = await pmClient.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTaskDto);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
            Assert.NotNull(createdTask);
            Assert.Equal("New Integration Task", createdTask.Title);
            Assert.Equal(member.Id, createdTask.AssigneeId);

            // 3. Verify Audit Log was written for Task Creation
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var auditLog = await db.AuditLogs.FirstOrDefaultAsync(al => al.EntityType == "Task" && al.EntityId == createdTask.Id.ToString() && al.Action == "TaskCreated");
                Assert.NotNull(auditLog);
                Assert.Equal(pm.Id, auditLog.ChangedById);
            }

            // 4. Stranger attempts to create task in this project -> Should get 403 Forbidden (RequireProjectMembership)
            var strangerCreateResponse = await strangerClient.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTaskDto);
            Assert.Equal(HttpStatusCode.Forbidden, strangerCreateResponse.StatusCode);

            // 5. Regular Member (assignee) attempts to update status of their task -> Should succeed
            var updateStatusDto = new UpdateTaskStatusDto { Status = "InProgress" };
            var updateStatusResponse = await memberClient.PatchAsJsonAsync($"/api/tasks/{createdTask.Id}/status", updateStatusDto);
            Assert.Equal(HttpStatusCode.OK, updateStatusResponse.StatusCode);

            // Verify status changed in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var taskInDb = await db.Tasks.FindAsync(createdTask.Id);
                Assert.Equal(TaskStatus.InProgress, taskInDb.Status);

                // Verify Audit Log for Status Change
                var statusAudit = await db.AuditLogs.FirstOrDefaultAsync(al => al.EntityType == "Task" && al.EntityId == createdTask.Id.ToString() && al.Action == "TaskStatusChanged");
                Assert.NotNull(statusAudit);
                Assert.Equal("Todo", statusAudit.OldValue);
                Assert.Equal("InProgress", statusAudit.NewValue);
            }

            // 6. Member attempts to perform full update (including title, which they are not allowed to edit) -> Should get 403 Forbidden
            var fullUpdateDto = new UpdateTaskDto
            {
                Title = "Modified Title By Member",
                Description = "New description",
                Status = "InReview",
                Priority = "Critical",
                AssigneeId = member.Id,
                DueDate = DateTime.UtcNow.AddDays(5),
                RowVersion = createdTask.RowVersion
            };
            var fullUpdateResponse = await memberClient.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", fullUpdateDto);
            Assert.Equal(HttpStatusCode.Forbidden, fullUpdateResponse.StatusCode);

            // 7. PM attempts to perform full update -> Should succeed
            // Need to get latest RowVersion first
            var getTaskResponse = await pmClient.GetAsync($"/api/tasks/{createdTask.Id}");
            var latestTaskDto = await getTaskResponse.Content.ReadFromJsonAsync<TaskDto>();
            fullUpdateDto.RowVersion = latestTaskDto.RowVersion;

            var pmUpdateResponse = await pmClient.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", fullUpdateDto);
            Assert.Equal(HttpStatusCode.OK, pmUpdateResponse.StatusCode);

            // 8. Test Optimistic Concurrency: Update task using outdated RowVersion -> Should get 409 Conflict
            var conflictUpdateDto = new UpdateTaskDto
            {
                Title = "Outdated Update",
                Description = "New details",
                Status = "Done",
                Priority = "Critical",
                AssigneeId = member.Id,
                DueDate = DateTime.UtcNow.AddDays(5),
                RowVersion = latestTaskDto.RowVersion // Outdated because PM already updated it in step 7!
            };
            var conflictResponse = await pmClient.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", conflictUpdateDto);
            Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

            // 9. Member attempts to delete task -> Should get 403 Forbidden
            var deleteResponse = await memberClient.DeleteAsync($"/api/tasks/{createdTask.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

            // 10. PM deletes task -> Should succeed (soft-delete)
            var pmDeleteResponse = await pmClient.DeleteAsync($"/api/tasks/{createdTask.Id}");
            Assert.Equal(HttpStatusCode.OK, pmDeleteResponse.StatusCode);

            // Verify soft deleted in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var taskInDb = await db.Tasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == createdTask.Id);
                Assert.NotNull(taskInDb);
                Assert.True(taskInDb.IsDeleted);

                // Verify Audit Log for Task Deletion
                var deleteAudit = await db.AuditLogs.FirstOrDefaultAsync(al => al.EntityType == "Task" && al.EntityId == createdTask.Id.ToString() && al.Action == "TaskDeleted");
                Assert.NotNull(deleteAudit);
            }
        }

        [Fact]
        public async Task FileAttachment_UploadAndDownload_Flow_ShouldBeAuthorized()
        {
            var (project, pm, member, stranger) = await SeedDataAsync();

            var pmClient = GetAuthenticatedClient(pm, "ProjectManager");
            var memberClient = GetAuthenticatedClient(member, "Member");
            var strangerClient = GetAuthenticatedClient(stranger, "Member");

            // Seed a task first
            Guid taskId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var task = new DomainTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Upload Test Task", CreatedById = pm.Id };
                db.Tasks.Add(task);
                await db.SaveChangesAsync();
                taskId = task.Id;
            }

            // 1. Member uploads an attachment
            using var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            using var formData = new MultipartFormDataContent();
            formData.Add(fileContent, "file", "test.png");

            var uploadResponse = await memberClient.PostAsync($"/api/tasks/{taskId}/attachments", formData);
            Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

            // Verify attachment created in DB
            Guid attachmentId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var attachment = await db.TaskAttachments.FirstOrDefaultAsync(ta => ta.TaskId == taskId && ta.FileName == "test.png");
                Assert.NotNull(attachment);
                attachmentId = attachment.Id;
            }

            // 2. Stranger attempts to download -> Should get 403 Forbidden
            var strangerDownloadResponse = await strangerClient.GetAsync($"/api/attachments/{attachmentId}/download");
            Assert.Equal(HttpStatusCode.Forbidden, strangerDownloadResponse.StatusCode);

            // 3. Member downloads -> Should succeed (200 OK)
            var downloadResponse = await memberClient.GetAsync($"/api/attachments/{attachmentId}/download");
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        }
    }
}

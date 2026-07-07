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
    public class TaskRelationshipIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public TaskRelationshipIntegrationTests(CustomWebApplicationFactory<Program> factory)
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

        private async Task<(Project project, User pm, User stranger)> SeedDataAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Clean up DB first
            db.Tasks.RemoveRange(db.Tasks);
            db.ProjectMembers.RemoveRange(db.ProjectMembers);
            db.Projects.RemoveRange(db.Projects);
            db.Users.RemoveRange(db.Users);
            await db.SaveChangesAsync();

            var pm = new User { Id = Guid.NewGuid(), Email = $"pm_{Guid.NewGuid()}@test.com", FullName = "Project Manager", Status = UserStatus.Active };
            var stranger = new User { Id = Guid.NewGuid(), Email = $"stranger_{Guid.NewGuid()}@test.com", FullName = "Stranger User", Status = UserStatus.Active };

            db.Users.AddRange(pm, stranger);
            await db.SaveChangesAsync();

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Task Relationships Project",
                Description = "A project created for testing relationships",
                OwnerId = pm.Id,
                Status = ProjectStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = pm.Id,
                RoleInProject = ProjectMemberRole.ProjectManager,
                Status = ProjectMemberStatus.Active,
                JoinedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            return (project, pm, stranger);
        }

        [Fact]
        public async Task Task_Relationships_AllConstraints_ShouldBeEnforced()
        {
            var (project, pm, stranger) = await SeedDataAsync();
            var pmClient = GetAuthenticatedClient(pm, "ProjectManager");
            var strangerClient = GetAuthenticatedClient(stranger, "Member");

            // Seed tasks in same project
            Guid taskAId;
            Guid taskBId;
            Guid taskCId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tA = new DomainTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Task A", CreatedById = pm.Id, Status = TaskStatus.Todo, Priority = TaskPriority.Medium };
                var tB = new DomainTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Task B", CreatedById = pm.Id, Status = TaskStatus.Todo, Priority = TaskPriority.Medium };
                var tC = new DomainTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Task C", CreatedById = pm.Id, Status = TaskStatus.Todo, Priority = TaskPriority.Medium };

                db.Tasks.AddRange(tA, tB, tC);
                await db.SaveChangesAsync();

                taskAId = tA.Id;
                taskBId = tB.Id;
                taskCId = tC.Id;
            }

            // Seed a task in another project to test project isolation
            Guid otherProjectTaskId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var otherProject = new Project
                {
                    Id = Guid.NewGuid(),
                    Name = "Other Project",
                    OwnerId = pm.Id,
                    Status = ProjectStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Projects.Add(otherProject);

                var tOther = new DomainTask { Id = Guid.NewGuid(), ProjectId = otherProject.Id, Title = "Other Task", CreatedById = pm.Id, Status = TaskStatus.Todo, Priority = TaskPriority.Medium };
                db.Tasks.Add(tOther);
                await db.SaveChangesAsync();

                otherProjectTaskId = tOther.Id;
            }

            // 1. Set Task B's parent to Task A -> Should succeed
            var setParentDto = new SetParentTaskDto { ParentTaskId = taskAId };
            var setParentResponse = await pmClient.PatchAsJsonAsync($"/api/tasks/{taskBId}/parent", setParentDto);
            Assert.Equal(HttpStatusCode.OK, setParentResponse.StatusCode);

            // Verify db updated and audit log written
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tB = await db.Tasks.FindAsync(taskBId);
                Assert.Equal(taskAId, tB.ParentTaskId);

                var audit = await db.AuditLogs.FirstOrDefaultAsync(al => al.EntityType == "Task" && al.EntityId == taskBId.ToString() && al.Action == "TaskParentChanged");
                Assert.NotNull(audit);
                Assert.Equal(taskAId.ToString(), audit.NewValue);
            }

            // 2. Set Task C's parent to Task B -> Should succeed (A -> B -> C hierarchy)
            var setParentCtoBResponse = await pmClient.PatchAsJsonAsync($"/api/tasks/{taskCId}/parent", new SetParentTaskDto { ParentTaskId = taskBId });
            Assert.Equal(HttpStatusCode.OK, setParentCtoBResponse.StatusCode);

            // 3. Try to set Task A's parent to Task C (creates circular A -> B -> C -> A) -> Should fail with 400 Bad Request
            var setParentAToCResponse = await pmClient.PatchAsJsonAsync($"/api/tasks/{taskAId}/parent", new SetParentTaskDto { ParentTaskId = taskCId });
            Assert.Equal(HttpStatusCode.BadRequest, setParentAToCResponse.StatusCode);

            // 4. Try to set Task A's parent to Task A (self parent) -> Should fail with 400 Bad Request
            var selfParentResponse = await pmClient.PatchAsJsonAsync($"/api/tasks/{taskAId}/parent", new SetParentTaskDto { ParentTaskId = taskAId });
            Assert.Equal(HttpStatusCode.BadRequest, selfParentResponse.StatusCode);

            // 5. Try to set Task A's parent to task in other project -> Should fail with 400 Bad Request
            var otherProjectParentResponse = await pmClient.PatchAsJsonAsync($"/api/tasks/{taskAId}/parent", new SetParentTaskDto { ParentTaskId = otherProjectTaskId });
            Assert.Equal(HttpStatusCode.BadRequest, otherProjectParentResponse.StatusCode);

            // 6. Stranger attempts to set parent -> Should fail with 403 Forbidden
            var strangerSetParentResponse = await strangerClient.PatchAsJsonAsync($"/api/tasks/{taskBId}/parent", setParentDto);
            Assert.Equal(HttpStatusCode.Forbidden, strangerSetParentResponse.StatusCode);

            // 7. Get Task A's children -> Should return Task B
            var getChildrenResponse = await pmClient.GetAsync($"/api/tasks/{taskAId}/children");
            Assert.Equal(HttpStatusCode.OK, getChildrenResponse.StatusCode);
            var children = await getChildrenResponse.Content.ReadFromJsonAsync<List<SubTaskDto>>();
            Assert.NotNull(children);
            Assert.Single(children);
            Assert.Equal(taskBId, children[0].Id);

            // 8. Stranger tries to get Task A's children -> Should fail with 403 Forbidden
            var strangerGetChildrenResponse = await strangerClient.GetAsync($"/api/tasks/{taskAId}/children");
            Assert.Equal(HttpStatusCode.Forbidden, strangerGetChildrenResponse.StatusCode);

            // 9. Try to delete Task A (parent of Task B which is Todo status) -> Should fail with 400 Bad Request
            var deleteParentResponse = await pmClient.DeleteAsync($"/api/tasks/{taskAId}");
            Assert.Equal(HttpStatusCode.BadRequest, deleteParentResponse.StatusCode);

            // 10. Update Task B and Task C status to Done and Cancelled
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tB = await db.Tasks.FindAsync(taskBId);
                tB.Status = TaskStatus.Done;
                var tC = await db.Tasks.FindAsync(taskCId);
                tC.Status = TaskStatus.Cancelled;
                await db.SaveChangesAsync();
            }

            // 11. Try to delete Task A now (all children are Done/Cancelled) -> Should succeed
            var deleteParentSuccessResponse = await pmClient.DeleteAsync($"/api/tasks/{taskAId}");
            Assert.Equal(HttpStatusCode.OK, deleteParentSuccessResponse.StatusCode);

            // Verify A is deleted but B is NOT deleted
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tA = await db.Tasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == taskAId);
                Assert.True(tA.IsDeleted);

                var tB = await db.Tasks.FindAsync(taskBId);
                Assert.NotNull(tB);
                Assert.False(tB.IsDeleted);
            }

            // 12. Remove parent of Task B (Task B has parent A which is now deleted/soft-deleted) -> Should succeed
            var removeParentResponse = await pmClient.PatchAsync($"/api/tasks/{taskBId}/remove-parent", null);
            Assert.Equal(HttpStatusCode.OK, removeParentResponse.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tB = await db.Tasks.FindAsync(taskBId);
                Assert.Null(tB.ParentTaskId);

                var audit = await db.AuditLogs.FirstOrDefaultAsync(al => al.EntityType == "Task" && al.EntityId == taskBId.ToString() && al.Action == "TaskParentRemoved");
                Assert.NotNull(audit);
                Assert.Equal(taskAId.ToString(), audit.OldValue);
            }
        }
    }
}

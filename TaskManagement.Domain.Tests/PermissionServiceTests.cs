using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using DomainTask = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using Task = System.Threading.Tasks.Task;

namespace TaskManagement.Domain.Tests
{
    public class PermissionServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task AdminUser_ShouldHaveAccessToEverything()
        {
            // Arrange
            var db = CreateDbContext();
            var adminId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            // Seed Admin role
            db.Roles.Add(new Role { Id = 1, Name = "Admin" });
            db.Users.Add(new User { Id = adminId, Email = "admin@test.com" });
            db.UserRoles.Add(new UserRole { UserId = adminId, RoleId = 1 });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            Assert.True(await service.CanViewProjectAsync(adminId, projectId));
            Assert.True(await service.CanEditProjectAsync(adminId, projectId));
            Assert.True(await service.CanDeleteProjectAsync(adminId, projectId));
            Assert.True(await service.CanManageProjectMembersAsync(adminId, projectId));
            Assert.True(await service.CanCreateTaskAsync(adminId, projectId));
            Assert.True(await service.CanViewAuditLogAsync(adminId, projectId));
        }

        [Fact]
        public async Task ProjectManager_ShouldManageProjectButNotDeleteIt()
        {
            // Arrange
            var db = CreateDbContext();
            var pmId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            db.Projects.Add(new Project { Id = projectId, Name = "Project A", OwnerId = Guid.NewGuid() });
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                UserId = pmId,
                RoleInProject = ProjectMemberRole.ProjectManager,
                Status = ProjectMemberStatus.Active
            });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            Assert.True(await service.CanViewProjectAsync(pmId, projectId));
            Assert.True(await service.CanEditProjectAsync(pmId, projectId));
            Assert.True(await service.CanManageProjectMembersAsync(pmId, projectId));
            Assert.True(await service.CanCreateTaskAsync(pmId, projectId));
            Assert.True(await service.CanViewAuditLogAsync(pmId, projectId));
            
            // Cannot delete project
            Assert.False(await service.CanDeleteProjectAsync(pmId, projectId));
        }

        [Fact]
        public async Task MemberRole_ShouldHaveLimitedAccess()
        {
            // Arrange
            var db = CreateDbContext();
            var memberId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            db.Projects.Add(new Project { Id = projectId, Name = "Project A", OwnerId = Guid.NewGuid() });
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                UserId = memberId,
                RoleInProject = ProjectMemberRole.Member,
                Status = ProjectMemberStatus.Active
            });
            db.Tasks.Add(new DomainTask
            {
                Id = taskId,
                ProjectId = projectId,
                Title = "Task A",
                AssigneeId = memberId,
                CreatedById = Guid.NewGuid()
            });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            Assert.True(await service.CanViewProjectAsync(memberId, projectId));
            Assert.True(await service.CanCreateTaskAsync(memberId, projectId));
            Assert.True(await service.CanCommentOnTaskAsync(memberId, taskId));

            // Can edit own task (description / status)
            Assert.True(await service.CanEditOwnTaskAsync(memberId, taskId));

            // Cannot manage members, delete project, full edit task, or delete task
            Assert.False(await service.CanEditProjectAsync(memberId, projectId));
            Assert.False(await service.CanManageProjectMembersAsync(memberId, projectId));
            Assert.False(await service.CanEditTaskAsync(memberId, taskId));
            Assert.False(await service.CanDeleteTaskAsync(memberId, taskId));
            Assert.False(await service.CanViewAuditLogAsync(memberId, projectId));
        }

        [Fact]
        public async Task GuestRole_ShouldOnlyViewAndComment()
        {
            // Arrange
            var db = CreateDbContext();
            var guestId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            db.Projects.Add(new Project { Id = projectId, Name = "Project A", OwnerId = Guid.NewGuid() });
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                UserId = guestId,
                RoleInProject = ProjectMemberRole.Guest,
                Status = ProjectMemberStatus.Active
            });
            db.Tasks.Add(new DomainTask
            {
                Id = taskId,
                ProjectId = projectId,
                Title = "Task A",
                CreatedById = Guid.NewGuid()
            });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            Assert.True(await service.CanViewProjectAsync(guestId, projectId));
            Assert.True(await service.CanViewTaskAsync(guestId, taskId));
            Assert.True(await service.CanCommentOnTaskAsync(guestId, taskId));

            // Cannot create task, edit task, or manage project
            Assert.False(await service.CanCreateTaskAsync(guestId, projectId));
            Assert.False(await service.CanEditOwnTaskAsync(guestId, taskId));
            Assert.False(await service.CanEditTaskAsync(guestId, taskId));
            Assert.False(await service.CanUploadAttachmentAsync(guestId, taskId));
        }

        [Fact]
        public async Task NonMember_ShouldBeDeniedAccess()
        {
            // Arrange
            var db = CreateDbContext();
            var strangerId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            db.Projects.Add(new Project { Id = projectId, Name = "Project A", OwnerId = Guid.NewGuid() });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            Assert.False(await service.CanViewProjectAsync(strangerId, projectId));
            Assert.False(await service.CanCreateTaskAsync(strangerId, projectId));
        }

        [Fact]
        public async Task ProjectOwner_ShouldHaveFullProjectAccess()
        {
            // Arrange
            var db = CreateDbContext();
            var ownerId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            db.Projects.Add(new Project { Id = projectId, Name = "Project A", OwnerId = ownerId });
            db.Tasks.Add(new DomainTask { Id = taskId, ProjectId = projectId, Title = "Task A", CreatedById = ownerId });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            Assert.True(await service.CanViewProjectAsync(ownerId, projectId));
            Assert.True(await service.CanEditProjectAsync(ownerId, projectId));
            Assert.True(await service.CanManageProjectMembersAsync(ownerId, projectId));
            Assert.True(await service.CanCreateTaskAsync(ownerId, projectId));
            Assert.True(await service.CanEditTaskAsync(ownerId, taskId));
            Assert.True(await service.CanDeleteTaskAsync(ownerId, taskId));
        }

        [Fact]
        public async Task InactiveMember_ShouldBeDeniedAccess()
        {
            // Arrange
            var db = CreateDbContext();
            var memberId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            db.Projects.Add(new Project { Id = projectId, Name = "Project A", OwnerId = Guid.NewGuid() });
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                UserId = memberId,
                RoleInProject = ProjectMemberRole.Member,
                Status = ProjectMemberStatus.Inactive
            });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            Assert.False(await service.CanViewProjectAsync(memberId, projectId));
            Assert.False(await service.CanCreateTaskAsync(memberId, projectId));
        }

        [Fact]
        public async Task CommentPermissions_ShouldEnforceRules()
        {
            // Arrange
            var db = CreateDbContext();
            var pmId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var commentId1 = Guid.NewGuid();
            var commentId2 = Guid.NewGuid();

            db.Projects.Add(new Project { Id = projectId, Name = "Project A", OwnerId = Guid.NewGuid() });
            db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = pmId, RoleInProject = ProjectMemberRole.ProjectManager, Status = ProjectMemberStatus.Active });
            db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = memberId, RoleInProject = ProjectMemberRole.Member, Status = ProjectMemberStatus.Active });
            db.Tasks.Add(new DomainTask { Id = taskId, ProjectId = projectId, Title = "Task A", CreatedById = pmId });
            db.TaskComments.Add(new TaskComment { Id = commentId1, TaskId = taskId, UserId = memberId, Content = "Member's comment" });
            db.TaskComments.Add(new TaskComment { Id = commentId2, TaskId = taskId, UserId = pmId, Content = "PM's comment" });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            // Member can delete own comment, but not PM's comment
            Assert.True(await service.CanDeleteCommentAsync(memberId, commentId1));
            Assert.False(await service.CanDeleteCommentAsync(memberId, commentId2));

            // PM can delete any comment in the project
            Assert.True(await service.CanDeleteCommentAsync(pmId, commentId1));
            Assert.True(await service.CanDeleteCommentAsync(pmId, commentId2));
        }

        [Fact]
        public async Task AttachmentPermissions_ShouldEnforceRules()
        {
            // Arrange
            var db = CreateDbContext();
            var memberId = Guid.NewGuid();
            var guestId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var attachmentId = Guid.NewGuid();

            db.Projects.Add(new Project { Id = projectId, Name = "Project A", OwnerId = Guid.NewGuid() });
            db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = memberId, RoleInProject = ProjectMemberRole.Member, Status = ProjectMemberStatus.Active });
            db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = guestId, RoleInProject = ProjectMemberRole.Guest, Status = ProjectMemberStatus.Active });
            db.Tasks.Add(new DomainTask { Id = taskId, ProjectId = projectId, Title = "Task A", CreatedById = Guid.NewGuid() });
            db.TaskAttachments.Add(new TaskAttachment
            {
                Id = attachmentId,
                TaskId = taskId,
                UploadedById = memberId,
                FileName = "test.png",
                StorageKey = "test_key",
                ContentType = "image/png",
                FileSize = 100
            });
            await db.SaveChangesAsync();

            var service = new PermissionService(db);

            // Act & Assert
            // Member can upload
            Assert.True(await service.CanUploadAttachmentAsync(memberId, taskId));
            // Guest cannot upload
            Assert.False(await service.CanUploadAttachmentAsync(guestId, taskId));

            // Both can view/download
            Assert.True(await service.CanDownloadAttachmentAsync(memberId, attachmentId));
            Assert.True(await service.CanDownloadAttachmentAsync(guestId, attachmentId));
        }
    }
}

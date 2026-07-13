using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IAppDbContext _dbContext;

        public PermissionService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private async Task<bool> IsAdminAsync(Guid userId)
        {
            // RoleId 1 is the Admin system role seeded in Phase 1
            return await _dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == 1);
        }

        private async Task<ProjectMemberRole?> GetProjectRoleAsync(Guid userId, Guid projectId)
        {
            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && pm.Status == ProjectMemberStatus.Active);

            return member?.RoleInProject;
        }

        private async Task<bool> IsProjectOwnerAsync(Guid userId, Guid projectId)
        {
            var project = await _dbContext.Projects.FindAsync(projectId);
            return project != null && project.OwnerId == userId;
        }

        public async Task<bool> CanViewProjectAsync(Guid userId, Guid projectId)
        {
            if (await IsAdminAsync(userId)) return true;
            if (await IsProjectOwnerAsync(userId, projectId)) return true;

            var role = await GetProjectRoleAsync(userId, projectId);
            return role != null; // PM, Member, Guest are members and can view
        }

        public async Task<bool> CanEditProjectAsync(Guid userId, Guid projectId)
        {
            if (await IsAdminAsync(userId)) return true;
            if (await IsProjectOwnerAsync(userId, projectId)) return true;

            var role = await GetProjectRoleAsync(userId, projectId);
            return role == ProjectMemberRole.ProjectManager; // PMs can edit project details
        }

        public async Task<bool> CanDeleteProjectAsync(Guid userId, Guid projectId)
        {
            return await IsAdminAsync(userId); // Admins only can delete projects
        }

        public async Task<bool> CanManageProjectMembersAsync(Guid userId, Guid projectId)
        {
            if (await IsAdminAsync(userId)) return true;
            if (await IsProjectOwnerAsync(userId, projectId)) return true;

            var role = await GetProjectRoleAsync(userId, projectId);
            return role == ProjectMemberRole.ProjectManager; // PMs and Owners can manage project members
        }

        public async Task<bool> CanViewTaskAsync(Guid userId, Guid taskId)
        {
            if (await IsAdminAsync(userId)) return true;

            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null) return false;

            return await CanViewProjectAsync(userId, task.ProjectId);
        }

        public async Task<bool> CanCreateTaskAsync(Guid userId, Guid projectId)
        {
            if (await IsAdminAsync(userId)) return true;
            if (await IsProjectOwnerAsync(userId, projectId)) return true;

            var role = await GetProjectRoleAsync(userId, projectId);
            return role == ProjectMemberRole.ProjectManager || role == ProjectMemberRole.Member; // PMs and Members can create tasks
        }

        public async Task<bool> CanEditTaskAsync(Guid userId, Guid taskId)
        {
            if (await IsAdminAsync(userId)) return true;

            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null) return false;

            if (await IsProjectOwnerAsync(userId, task.ProjectId)) return true;

            var role = await GetProjectRoleAsync(userId, task.ProjectId);
            return role == ProjectMemberRole.ProjectManager; // PMs and Admins can edit all attributes
        }

        public async Task<bool> CanEditOwnTaskAsync(Guid userId, Guid taskId)
        {
            if (await IsAdminAsync(userId)) return true;

            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null) return false;

            if (await IsProjectOwnerAsync(userId, task.ProjectId)) return true;

            var role = await GetProjectRoleAsync(userId, task.ProjectId);
            if (role == ProjectMemberRole.ProjectManager) return true;
            
            if (role == ProjectMemberRole.Member)
            {
                return task.AssigneeId == userId; // Members can only edit description/status of their assigned tasks
            }

            return false;
        }

        public async Task<bool> CanDeleteTaskAsync(Guid userId, Guid taskId)
        {
            if (await IsAdminAsync(userId)) return true;

            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null) return false;

            if (await IsProjectOwnerAsync(userId, task.ProjectId)) return true;

            var role = await GetProjectRoleAsync(userId, task.ProjectId);
            return role == ProjectMemberRole.ProjectManager; // PMs and Admins can delete tasks
        }

        public async Task<bool> CanCommentOnTaskAsync(Guid userId, Guid taskId)
        {
            if (await IsAdminAsync(userId)) return true;

            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null) return false;

            return await CanViewProjectAsync(userId, task.ProjectId); // Anyone who can view can comment
        }

        public async Task<bool> CanDeleteCommentAsync(Guid userId, Guid commentId)
        {
            if (await IsAdminAsync(userId)) return true;

            var comment = await _dbContext.TaskComments.FindAsync(commentId);
            if (comment == null) return false;

            if (comment.UserId == userId) return true; // Owners can delete their own comments

            var task = await _dbContext.Tasks.FindAsync(comment.TaskId);
            if (task == null) return false;

            var role = await GetProjectRoleAsync(userId, task.ProjectId);
            return role == ProjectMemberRole.ProjectManager; // PMs can delete other users' comments
        }

        public async Task<bool> CanUploadAttachmentAsync(Guid userId, Guid taskId)
        {
            if (await IsAdminAsync(userId)) return true;

            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null) return false;

            if (await IsProjectOwnerAsync(userId, task.ProjectId)) return true;

            var role = await GetProjectRoleAsync(userId, task.ProjectId);
            return role == ProjectMemberRole.ProjectManager || role == ProjectMemberRole.Member; // PMs and Members can upload attachments
        }

        public async Task<bool> CanDownloadAttachmentAsync(Guid userId, Guid attachmentId)
        {
            if (await IsAdminAsync(userId)) return true;

            var attachment = await _dbContext.TaskAttachments.FindAsync(attachmentId);
            if (attachment == null) return false;

            var task = await _dbContext.Tasks.FindAsync(attachment.TaskId);
            if (task == null) return false;

            return await CanViewProjectAsync(userId, task.ProjectId); // Any member who can view task can download attachments
        }

        public async Task<bool> CanViewAuditLogAsync(Guid userId, Guid projectId)
        {
            if (await IsAdminAsync(userId)) return true;
            if (await IsProjectOwnerAsync(userId, projectId)) return true;

            var role = await GetProjectRoleAsync(userId, projectId);
            return role == ProjectMemberRole.ProjectManager; // PMs and Admins can view audit logs
        }

        private async Task<bool> HasSystemRoleAsync(Guid userId, params string[] roleNames)
        {
            return await _dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == userId && roleNames.Contains(ur.Role.Name));
        }

        public async Task<bool> CanManageProductsAsync(Guid userId)
        {
            return await IsAdminAsync(userId) || await HasSystemRoleAsync(userId, "Inventory Manager");
        }

        public async Task<bool> CanViewProductsAsync(Guid userId)
        {
            return await IsAdminAsync(userId) || 
                   await HasSystemRoleAsync(userId, "Inventory Manager", "Warehouse Staff", "Viewer");
        }
    }
}

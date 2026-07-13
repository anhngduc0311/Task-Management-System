using System;
using System.Threading.Tasks;

namespace TaskManagement.Application.Interfaces
{
    public interface IPermissionService
    {
        Task<bool> CanViewProjectAsync(Guid userId, Guid projectId);
        Task<bool> CanEditProjectAsync(Guid userId, Guid projectId);
        Task<bool> CanDeleteProjectAsync(Guid userId, Guid projectId);
        Task<bool> CanManageProjectMembersAsync(Guid userId, Guid projectId);
        
        Task<bool> CanViewTaskAsync(Guid userId, Guid taskId);
        Task<bool> CanCreateTaskAsync(Guid userId, Guid projectId);
        Task<bool> CanEditTaskAsync(Guid userId, Guid taskId); // Full edits (Admin, PM)
        Task<bool> CanEditOwnTaskAsync(Guid userId, Guid taskId); // Limited edits (Member assignee)
        Task<bool> CanDeleteTaskAsync(Guid userId, Guid taskId);
        
        Task<bool> CanCommentOnTaskAsync(Guid userId, Guid taskId);
        Task<bool> CanDeleteCommentAsync(Guid userId, Guid commentId);
        
        Task<bool> CanUploadAttachmentAsync(Guid userId, Guid taskId);
        Task<bool> CanDownloadAttachmentAsync(Guid userId, Guid attachmentId);
        
        Task<bool> CanViewAuditLogAsync(Guid userId, Guid projectId);

        Task<bool> CanManageProductsAsync(Guid userId);
        Task<bool> CanViewProductsAsync(Guid userId);
        Task<bool> CanManageWarehouseReceiptsAsync(Guid userId);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class CommentsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IAuditService _auditService;

        public CommentsController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _auditService = auditService;
        }

        [HttpGet("tasks/{taskId}/comments")]
        public async Task<IActionResult> GetTaskComments(Guid taskId)
        {
            var canView = await _permissionService.CanViewTaskAsync(CurrentUserId, taskId);
            if (!canView)
            {
                return Forbid();
            }

            var comments = await _dbContext.TaskComments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new TaskCommentDto
                {
                    Id = c.Id,
                    TaskId = c.TaskId,
                    UserId = c.UserId,
                    UserFullName = c.User.FullName,
                    UserAvatarUrl = c.User.AvatarUrl,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost("tasks/{taskId}/comments")]
        public async Task<IActionResult> CreateComment(Guid taskId, [FromBody] CreateCommentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var canComment = await _permissionService.CanCommentOnTaskAsync(CurrentUserId, taskId);
            if (!canComment)
            {
                return Forbid();
            }

            var taskExists = await _dbContext.Tasks.AnyAsync(t => t.Id == taskId && !t.IsDeleted);
            if (!taskExists)
            {
                return NotFound(new { message = "Task not found." });
            }

            // Sanitize content to avoid script injections
            var sanitizedContent = WebUtility.HtmlEncode(dto.Content.Trim());

            var comment = new TaskComment
            {
                TaskId = taskId,
                UserId = CurrentUserId,
                Content = sanitizedContent,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.TaskComments.Add(comment);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "TaskComment",
                entityId: comment.Id.ToString(),
                action: "CommentAdded",
                changedById: CurrentUserId,
                newValue: sanitizedContent,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var user = await _dbContext.Users.FindAsync(CurrentUserId);

            var resultDto = new TaskCommentDto
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                UserId = comment.UserId,
                UserFullName = user?.FullName ?? "",
                UserAvatarUrl = user?.AvatarUrl,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };

            return CreatedAtAction(nameof(GetTaskComments), new { taskId = comment.TaskId }, resultDto);
        }

        [HttpPut("tasks/{taskId}/comments/{commentId}")]
        public async Task<IActionResult> UpdateComment(Guid taskId, Guid commentId, [FromBody] UpdateCommentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var comment = await _dbContext.TaskComments.FindAsync(commentId);
            if (comment == null || comment.IsDeleted || comment.TaskId != taskId)
            {
                return NotFound(new { message = "Comment not found." });
            }

            // Only the owner of the comment can edit
            if (comment.UserId != CurrentUserId)
            {
                return Forbid();
            }

            var oldValue = comment.Content;
            var sanitizedContent = WebUtility.HtmlEncode(dto.Content.Trim());

            try
            {
                comment.UpdateContent(sanitizedContent);
                await _dbContext.SaveChangesAsync();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            await _auditService.LogAsync(
                entityType: "TaskComment",
                entityId: comment.Id.ToString(),
                action: "CommentUpdated",
                changedById: CurrentUserId,
                oldValue: oldValue,
                newValue: sanitizedContent,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var user = await _dbContext.Users.FindAsync(CurrentUserId);

            return Ok(new TaskCommentDto
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                UserId = comment.UserId,
                UserFullName = user?.FullName ?? "",
                UserAvatarUrl = user?.AvatarUrl,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            });
        }

        [HttpDelete("tasks/{taskId}/comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(Guid taskId, Guid commentId)
        {
            var canDelete = await _permissionService.CanDeleteCommentAsync(CurrentUserId, commentId);
            if (!canDelete)
            {
                return Forbid();
            }

            var comment = await _dbContext.TaskComments.FindAsync(commentId);
            if (comment == null || comment.IsDeleted || comment.TaskId != taskId)
            {
                return NotFound(new { message = "Comment not found." });
            }

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "TaskComment",
                entityId: comment.Id.ToString(),
                action: "CommentDeleted",
                changedById: CurrentUserId,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Comment soft-deleted successfully." });
        }
    }
}

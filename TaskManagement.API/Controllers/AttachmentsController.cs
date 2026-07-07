using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Attachments;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class AttachmentsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;

        public AttachmentsController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IFileStorageService fileStorageService,
            IAuditService auditService,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _fileStorageService = fileStorageService;
            _auditService = auditService;
            _configuration = configuration;
        }

        [HttpPost("tasks/{taskId}/attachments")]
        [EnableRateLimiting("upload-limiter")]
        public async Task<IActionResult> UploadAttachment(Guid taskId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file was uploaded." });
            }

            var canUpload = await _permissionService.CanUploadAttachmentAsync(CurrentUserId, taskId);
            if (!canUpload)
            {
                return Forbid();
            }

            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            // Size validation
            var maxSizeBytes = _configuration.GetValue<long>("FileStorage:MaxFileSizeInBytes", 20971520); // Default 20MB
            if (file.Length > maxSizeBytes)
            {
                var maxSizeMb = maxSizeBytes / 1024 / 1024;
                return BadRequest(new { message = $"File size exceeds the limit of {maxSizeMb}MB." });
            }

            // Extension validation
            var allowedExtensions = _configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
                                    ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = $"Only image files are allowed ({string.Join(", ", allowedExtensions)})." });
            }

            // Save physically
            string storageKey;
            using (var stream = file.OpenReadStream())
            {
                storageKey = await _fileStorageService.SaveFileAsync(stream, file.FileName);
            }

            // Save in Database
            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                UploadedById = CurrentUserId,
                FileName = file.FileName,
                StorageKey = storageKey,
                ContentType = file.ContentType,
                FileSize = file.Length,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.TaskAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "TaskAttachment",
                entityId: attachment.Id.ToString(),
                action: "AttachmentUploaded",
                changedById: CurrentUserId,
                newValue: attachment.FileName,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            var uploader = await _dbContext.Users.FindAsync(CurrentUserId);

            var resultDto = new TaskAttachmentDto
            {
                Id = attachment.Id,
                TaskId = attachment.TaskId,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedById = attachment.UploadedById,
                UploadedByName = uploader?.FullName ?? "",
                CreatedAt = attachment.CreatedAt
            };

            return Ok(resultDto);
        }

        [HttpGet("tasks/{taskId}/attachments")]
        public async Task<IActionResult> GetTaskAttachments(Guid taskId)
        {
            var canView = await _permissionService.CanViewTaskAsync(CurrentUserId, taskId);
            if (!canView)
            {
                return Forbid();
            }

            var attachments = await _dbContext.TaskAttachments
                .Include(a => a.UploadedBy)
                .Where(a => a.TaskId == taskId && !a.IsDeleted)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new TaskAttachmentDto
                {
                    Id = a.Id,
                    TaskId = a.TaskId,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    UploadedById = a.UploadedById,
                    UploadedByName = a.UploadedBy.FullName,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(attachments);
        }

        [HttpGet("attachments/{id}/download")]
        public async Task<IActionResult> DownloadAttachment(Guid id)
        {
            var canDownload = await _permissionService.CanDownloadAttachmentAsync(CurrentUserId, id);
            if (!canDownload)
            {
                return Forbid();
            }

            var attachment = await _dbContext.TaskAttachments.FindAsync(id);
            if (attachment == null || attachment.IsDeleted)
            {
                return NotFound(new { message = "Attachment not found." });
            }

            try
            {
                var fileStream = await _fileStorageService.GetFileAsync(attachment.StorageKey);
                return File(fileStream, attachment.ContentType, attachment.FileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "Physical file not found in storage." });
            }
        }

        [HttpDelete("attachments/{id}")]
        public async Task<IActionResult> DeleteAttachment(Guid id)
        {
            var attachment = await _dbContext.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (attachment == null)
            {
                return NotFound(new { message = "Attachment not found." });
            }

            // PM/Admin or Uploader can delete
            var isUploader = attachment.UploadedById == CurrentUserId;
            var isAdmin = User.IsInRole("Admin");
            var isPM = false;
            var isOwner = false;

            if (!isUploader && !isAdmin)
            {
                var project = await _dbContext.Projects.FindAsync(attachment.Task.ProjectId);
                if (project != null)
                {
                    isOwner = project.OwnerId == CurrentUserId;
                    var member = await _dbContext.ProjectMembers
                        .FirstOrDefaultAsync(pm => pm.ProjectId == project.Id && pm.UserId == CurrentUserId && pm.Status == ProjectMemberStatus.Active);
                    isPM = member != null && member.RoleInProject == ProjectMemberRole.ProjectManager;
                }
            }

            if (!isUploader && !isAdmin && !isOwner && !isPM)
            {
                return Forbid();
            }

            // Set soft-deleted in Database
            attachment.IsDeleted = true;

            // Delete physical file
            await _fileStorageService.DeleteFileAsync(attachment.StorageKey);

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "TaskAttachment",
                entityId: attachment.Id.ToString(),
                action: "AttachmentDeleted",
                changedById: CurrentUserId,
                oldValue: attachment.FileName,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(new { message = "Attachment deleted successfully." });
        }
    }
}

using System;

namespace TaskManagement.Domain.Entities
{
    public class TaskAttachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TaskId { get; set; }
        public Task Task { get; set; } = null!;

        public Guid UploadedById { get; set; }
        public User UploadedBy { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

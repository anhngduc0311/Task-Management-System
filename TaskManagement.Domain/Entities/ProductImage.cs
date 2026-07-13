using System;

namespace TaskManagement.Domain.Entities
{
    public class ProductImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

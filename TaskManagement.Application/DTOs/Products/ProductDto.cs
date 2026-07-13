using System;

namespace TaskManagement.Application.DTOs.Products
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DefaultPrice { get; set; }
        public Guid BaseUnitId { get; set; }
        public string BaseUnitName { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? OriginId { get; set; }
        public string? OriginName { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}

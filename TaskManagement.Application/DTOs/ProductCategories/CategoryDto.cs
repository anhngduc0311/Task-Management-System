using System;

namespace TaskManagement.Application.DTOs.ProductCategories
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }
}

using System;

namespace TaskManagement.Application.DTOs.ProductLabels
{
    public class LabelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Color { get; set; }
        public bool IsActive { get; set; }
    }
}

using System;
using System.Collections.Generic;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class DynamicFieldDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string FieldName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public DynamicFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        
        public List<string> Options { get; set; } = new List<string>();
        public string? DefaultValue { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for task values
        public ICollection<TaskDynamicFieldValue> TaskDynamicFieldValues { get; set; } = new List<TaskDynamicFieldValue>();
    }
}

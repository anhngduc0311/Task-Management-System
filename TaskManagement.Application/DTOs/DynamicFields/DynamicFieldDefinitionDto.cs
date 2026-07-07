using System;
using System.Collections.Generic;

namespace TaskManagement.Application.DTOs.DynamicFields
{
    public class DynamicFieldDefinitionDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public string? DefaultValue { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

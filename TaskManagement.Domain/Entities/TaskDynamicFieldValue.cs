using System;

namespace TaskManagement.Domain.Entities
{
    public class TaskDynamicFieldValue
    {
        public Guid TaskId { get; set; }
        public Task Task { get; set; } = null!;

        public Guid DynamicFieldId { get; set; }
        public DynamicFieldDefinition DynamicFieldDefinition { get; set; } = null!;

        public string? FieldValue { get; set; }
    }
}

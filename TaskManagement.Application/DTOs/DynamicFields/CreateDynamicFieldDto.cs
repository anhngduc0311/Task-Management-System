using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.DynamicFields
{
    public class CreateDynamicFieldDto
    {
        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [RegularExpression("^[a-zA-Z][a-zA-Z0-9_]*$", ErrorMessage = "FieldKey must start with a letter and contain only alphanumeric characters and underscores.")]
        public string FieldKey { get; set; } = string.Empty;

        [Required]
        public string FieldType { get; set; } = string.Empty; // Text, Number, Date, Boolean, Select, MultiSelect

        public bool IsRequired { get; set; }

        public List<string>? Options { get; set; }

        public string? DefaultValue { get; set; }

        public int DisplayOrder { get; set; }
    }
}

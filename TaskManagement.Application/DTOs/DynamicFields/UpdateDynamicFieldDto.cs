using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.DynamicFields
{
    public class UpdateDynamicFieldDto
    {
        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        public bool IsRequired { get; set; }

        public List<string>? Options { get; set; }

        public string? DefaultValue { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

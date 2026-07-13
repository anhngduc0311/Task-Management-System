using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Origins
{
    public class CreateOriginDto
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}

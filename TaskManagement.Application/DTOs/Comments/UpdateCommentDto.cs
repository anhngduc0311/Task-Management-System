using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Comments
{
    public class UpdateCommentDto
    {
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}

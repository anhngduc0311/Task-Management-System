using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Users
{
    public class UpdateUserDto
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Url]
        [MaxLength(512)]
        public string? AvatarUrl { get; set; }
    }
}

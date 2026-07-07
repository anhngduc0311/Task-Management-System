using System.Collections.Generic;

namespace TaskManagement.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., Admin, ProjectManager, Member, Guest

        // Navigation Properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}

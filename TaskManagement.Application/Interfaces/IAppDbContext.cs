using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TaskManagement.Domain.Entities;
using Task = TaskManagement.Domain.Entities.Task;

namespace TaskManagement.Application.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Project> Projects { get; }
        DbSet<ProjectMember> ProjectMembers { get; }
        DbSet<Task> Tasks { get; }
        DbSet<TaskComment> TaskComments { get; }
        DbSet<TaskAttachment> TaskAttachments { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<DynamicFieldDefinition> DynamicFieldDefinitions { get; }
        DbSet<TaskDynamicFieldValue> TaskDynamicFieldValues { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using Task = TaskManagement.Domain.Entities.Task;

using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<Task> Tasks => Set<Task>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations in the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is TaskManagement.Domain.Entities.Task && 
                            (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var task = (TaskManagement.Domain.Entities.Task)entry.Entity;
                task.RowVersion = Guid.NewGuid().ToByteArray();
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}

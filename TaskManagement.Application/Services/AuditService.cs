using System;
using System.Threading.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAppDbContext _dbContext;

        public AuditService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async System.Threading.Tasks.Task LogAsync(
            string entityType,
            string entityId,
            string action,
            Guid changedById,
            string? oldValue = null,
            string? newValue = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                ChangedById = changedById,
                ChangedAt = DateTime.UtcNow,
                OldValue = oldValue,
                NewValue = newValue,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync();
        }
    }
}

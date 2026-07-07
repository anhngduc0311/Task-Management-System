using System;
using System.Threading.Tasks;

namespace TaskManagement.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(
            string entityType,
            string entityId,
            string action,
            Guid changedById,
            string? oldValue = null,
            string? newValue = null,
            string? ipAddress = null,
            string? userAgent = null);
    }
}

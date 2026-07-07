using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Health
{
    public class DbHealthCheck : IHealthCheck
    {
        private readonly IAppDbContext _dbContext;

        public DbHealthCheck(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // This checks query execution, which works on both SQL Server and EF InMemory
                await _dbContext.Users.AnyAsync(cancellationToken);
                return HealthCheckResult.Healthy("Database is responding.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed.", ex);
            }
        }
    }
}

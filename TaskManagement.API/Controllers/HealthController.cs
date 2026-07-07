using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly IAppDbContext _dbContext;

        public HealthController(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                // Verify DB connection works
                await _dbContext.Users.AnyAsync();
                
                return Ok(new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    database = "Connected"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow,
                    database = "Disconnected",
                    error = ex.Message
                });
            }
        }
    }
}

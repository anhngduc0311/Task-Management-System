using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        protected Guid CurrentUserId
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
            }
        }

        protected string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
        protected string? ClientUserAgent => HttpContext.Request.Headers["User-Agent"].ToString();
    }
}

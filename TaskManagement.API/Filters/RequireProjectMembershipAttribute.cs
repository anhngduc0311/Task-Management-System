using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireProjectMembershipAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user == null || !user.Identity!.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            Guid projectId = Guid.Empty;
            
            // Search routes first ("id", "pid", or "projectId")
            if (context.RouteData.Values.TryGetValue("id", out var pidObj) ||
                context.RouteData.Values.TryGetValue("pid", out pidObj) ||
                context.RouteData.Values.TryGetValue("projectId", out pidObj))
            {
                if (pidObj != null) Guid.TryParse(pidObj.ToString(), out projectId);
            }
            
            // Search query string next
            if (projectId == Guid.Empty && context.HttpContext.Request.Query.TryGetValue("projectId", out var queryPid))
            {
                Guid.TryParse(queryPid.ToString(), out projectId);
            }

            if (projectId == Guid.Empty)
            {
                context.Result = new BadRequestObjectResult("Project identifier is required.");
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var isMember = await permissionService.CanViewProjectAsync(userId, projectId);

            if (!isMember)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}

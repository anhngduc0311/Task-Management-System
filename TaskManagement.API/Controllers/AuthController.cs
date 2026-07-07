using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Services;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login-limiter")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _authService.LoginAsync(request);
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _authService.RefreshTokenAsync(request);
            if (response == null)
            {
                return BadRequest(new { message = "Invalid or expired token." });
            }

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized();
            }

            var success = await _authService.LogoutAsync(userId);
            if (!success) return BadRequest(new { message = "Failed to logout." });

            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized();
            }

            var success = await _authService.ChangePasswordAsync(userId, request);
            if (!success)
            {
                return BadRequest(new { message = "Invalid old password or failed password update." });
            }

            return Ok(new { message = "Password updated successfully." });
        }
    }
}

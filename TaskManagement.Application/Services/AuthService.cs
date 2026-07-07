using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services
{
    public class AuthService
    {
        private readonly IAppDbContext _dbContext;
        private readonly IJwtTokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(
            IAppDbContext dbContext,
            IJwtTokenService tokenService,
            IPasswordHasher passwordHasher)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.Status != Domain.Enums.UserStatus.Active)
            {
                return null;
            }

            if (user.PasswordHash == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return null;
            }

            // Retrieve user's system roles
            var roles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Set refresh token properties (expires in 7 days by default)
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var expiryMinutes = 15; // standard duration matching config/fallback
            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(expiryMinutes),
                FullName = user.FullName,
                Email = user.Email,
                UserId = user.Id,
                Roles = roles
            };
        }

        public async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                return null; // Invalid token structure
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return null;
            }

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null; // Invalid or expired refresh token
            }

            // Retrieve user's roles
            var roles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Rotate refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(15),
                FullName = user.FullName,
                Email = user.Email,
                UserId = user.Id,
                Roles = roles
            };
        }

        public async Task<bool> LogoutAsync(Guid userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null || user.PasswordHash == null) return false;

            if (!_passwordHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var exists = await _dbContext.Users.AnyAsync(u => u.Email == request.Email);
            if (exists) return false;

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Status = Domain.Enums.UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);

            var memberRole = await _dbContext.Roles.FindAsync(3); // 3 is "Member"
            if (memberRole != null)
            {
                _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = memberRole.Id });
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}

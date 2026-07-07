using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace TaskManagement.API.IntegrationTests
{
    public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AuthIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        private async Task<User> SeedUserAsync(string email, string password, string fullName)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // Clean existing if any
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null)
            {
                db.Users.Remove(existing);
                await db.SaveChangesAsync();
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = fullName,
                PasswordHash = hasher.HashPassword(password),
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnTokens()
        {
            // Arrange
            var email = $"login_valid_{Guid.NewGuid()}@test.com";
            var password = "SecurePassword123!";
            await SeedUserAsync(email, password, "Valid User");

            var request = new LoginRequest
            {
                Email = email,
                Password = password
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal("Valid User", result.FullName);
            Assert.Equal(email, result.Email);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var email = $"login_invalid_{Guid.NewGuid()}@test.com";
            await SeedUserAsync(email, "CorrectPassword123!", "Invalid User");

            var request = new LoginRequest
            {
                Email = email,
                Password = "WrongPassword!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_WithValidTokens_ShouldReturnNewTokens()
        {
            // Arrange
            var email = $"refresh_{Guid.NewGuid()}@test.com";
            var password = "SecurePassword123!";
            var user = await SeedUserAsync(email, password, "Refresh User");

            var loginRequest = new LoginRequest { Email = email, Password = password };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var loginBody = await loginResponse.Content.ReadAsStringAsync();
            Assert.True(loginResponse.IsSuccessStatusCode, $"Login failed with status {loginResponse.StatusCode}. Content: {loginBody}");
            var loginResult = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(loginBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var refreshRequest = new RefreshTokenRequest
            {
                AccessToken = loginResult.AccessToken,
                RefreshToken = loginResult.RefreshToken
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", refreshRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.NotEqual(loginResult.AccessToken, result.AccessToken);
        }

        [Fact]
        public async Task Logout_ShouldInvalidateRefreshToken()
        {
            // Arrange
            var email = $"logout_{Guid.NewGuid()}@test.com";
            var password = "SecurePassword123!";
            var user = await SeedUserAsync(email, password, "Logout User");

            var loginRequest = new LoginRequest { Email = email, Password = password };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var loginBody = await loginResponse.Content.ReadAsStringAsync();
            Assert.True(loginResponse.IsSuccessStatusCode, $"Login failed with status {loginResponse.StatusCode}. Content: {loginBody}");
            var loginResult = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(loginBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Setup authorization header
            var clientWithAuth = _factory.CreateClient();
            clientWithAuth.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

            // Act
            var response = await clientWithAuth.PostAsync("/api/auth/logout", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Double check refresh token is removed in DB
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = await db.Users.FindAsync(user.Id);
            Assert.Null(dbUser.RefreshToken);
        }
    }
}

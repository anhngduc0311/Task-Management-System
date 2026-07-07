using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TaskManagement.Infrastructure.Persistence;
using Xunit;

namespace TaskManagement.API.IntegrationTests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var optionsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (optionsDescriptor != null)
                {
                    services.Remove(optionsDescriptor);
                }

                services.AddScoped<DbContextOptions<AppDbContext>>(provider =>
                {
                    return new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase(_dbName)
                        .Options;
                });
            });
        }
    }

    public class SecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public SecurityIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnHealthyAndDatabaseConnected()
        {
            // Act
            var response = await _client.GetAsync("/health");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Health check failed with status {response.StatusCode}. Content: {content}");
            Assert.Contains("Healthy", content);
        }

        [Fact]
        public async Task SecurityHeaders_ShouldBePresentInResponse()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.True(response.Headers.Contains("X-Frame-Options"));
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());

            Assert.True(response.Headers.Contains("X-Content-Type-Options"));
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

            Assert.True(response.Headers.Contains("Referrer-Policy"));
            Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").First());

            Assert.True(response.Headers.Contains("Permissions-Policy"));
            Assert.Contains("geolocation=()", response.Headers.GetValues("Permissions-Policy").First());
        }

        [Theory]
        [InlineData("/api/projects")]
        [InlineData("/api/users")]
        [InlineData("/api/tasks/my-tasks")]
        public async Task SecureEndpoints_WithoutToken_ShouldReturnUnauthorized(string url)
        {
            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RateLimiting_OnLoginEndpoint_ShouldTriggerTooManyRequests()
        {
            // Send requests until rate limiting triggers (permit limit is 5 per minute)
            HttpResponseMessage? lastResponse = null;
            var requestContent = JsonContent.Create(new { Email = "test@example.com", Password = "Password123" });

            for (int i = 0; i < 7; i++)
            {
                // Re-create content since HttpClient disposes it
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
                {
                    Content = JsonContent.Create(new { Email = "test@example.com", Password = "Password123" })
                };
                lastResponse = await _client.SendAsync(request);
                if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    break;
                }
            }

            // Assert
            Assert.NotNull(lastResponse);
            Assert.Equal((HttpStatusCode)429, lastResponse.StatusCode);
            var errorContent = await lastResponse.Content.ReadAsStringAsync();
            Assert.Contains("Too many requests", errorContent);
        }
    }
}

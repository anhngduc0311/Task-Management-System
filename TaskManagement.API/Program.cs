using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Text;
using System.Threading.RateLimiting;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Storage;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Configure DB Context
if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("InMemoryDbForTesting"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            connectionString,
            b => b.MigrationsAssembly("TaskManagement.Infrastructure")));
}

// Register DB Context Abstraction
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// Register Auth & Security Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUnitConversionService, UnitConversionService>();
builder.Services.AddScoped<IHtmlSanitizer, HtmlSanitizer>();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<TaskManagement.API.Health.DbHealthCheck>("db");

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                     ?? new[] { "http://localhost:4200", "https://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many requests. Please try again later." }, cancellationToken: token);
    };

    var loginLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginPermitLimit", 5);
    var loginWindow = builder.Configuration.GetValue<int>("RateLimiting:LoginWindowInMinutes", 1);
    options.AddFixedWindowLimiter("login-limiter", opt =>
    {
        opt.PermitLimit = loginLimit;
        opt.Window = TimeSpan.FromMinutes(loginWindow);
        opt.QueueLimit = 0;
    });

    var uploadLimit = builder.Configuration.GetValue<int>("RateLimiting:UploadPermitLimit", 5);
    var uploadWindow = builder.Configuration.GetValue<int>("RateLimiting:UploadWindowInMinutes", 1);
    options.AddFixedWindowLimiter("upload-limiter", opt =>
    {
        opt.PermitLimit = uploadLimit;
        opt.Window = TimeSpan.FromMinutes(uploadWindow);
        opt.QueueLimit = 0;
    });

    var commentLimit = builder.Configuration.GetValue<int>("RateLimiting:CommentPermitLimit", 10);
    var commentWindow = builder.Configuration.GetValue<int>("RateLimiting:CommentWindowInMinutes", 1);
    options.AddFixedWindowLimiter("comment-limiter", opt =>
    {
        opt.PermitLimit = commentLimit;
        opt.Window = TimeSpan.FromMinutes(commentWindow);
        opt.QueueLimit = 0;
    });
});


// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"] 
                ?? throw new InvalidOperationException("JWT Secret Key is not configured."))),
        ClockSkew = TimeSpan.Zero
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Automatically apply migrations on startup (Production/Staging environments)
if (!builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            Log.Information("Applying pending database migrations...");
            context.Database.Migrate();
            Log.Information("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database.");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

// Custom Middleware: HTTP Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    
    if (context.Request.IsHttps)
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    var path = context.Request.Path.Value;
    if (path != null && !path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none';");
    }

    await next();
});

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("DefaultPolicy");

// Enable Rate Limiting
app.UseRateLimiter();

// Use Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map built-in Health Checks endpoint
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                exception = entry.Value.Exception?.Message
            }),
            duration = report.TotalDuration
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapControllers();

try
{
    Log.Information("Starting Task Management System Web API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

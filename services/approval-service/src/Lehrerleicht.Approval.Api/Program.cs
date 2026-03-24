using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Lehrerleicht.Approval.Api.Hubs;
using Lehrerleicht.Approval.Api.Middleware;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Interfaces;
using Lehrerleicht.Approval.Core.Services;
using Lehrerleicht.Approval.Infrastructure.BackgroundServices;
using Lehrerleicht.Approval.Infrastructure.Data;
using Lehrerleicht.Approval.Infrastructure.Messaging;
using Lehrerleicht.Approval.Infrastructure.Repositories;
using ApprovalSvc = Lehrerleicht.Approval.Core.Services.ApprovalService;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Database
builder.Services.AddDbContext<ApprovalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<Teacher, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApprovalDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = key
    };

    // Allow SignalR to receive the token via query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Repositories
builder.Services.AddScoped<IApprovalRepository, ApprovalRepository>();
builder.Services.AddScoped<IActionHistoryRepository, ActionHistoryRepository>();
builder.Services.AddScoped<ISchoolRepository, SchoolRepository>();
builder.Services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

// Services
builder.Services.AddScoped<ApprovalSvc>();
builder.Services.AddScoped<ActionHistoryService>();
builder.Services.AddScoped<NotificationService>();

// Messaging
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<RabbitMqPublisher>());
builder.Services.AddSingleton<RabbitMqConsumer>();

// Background services
builder.Services.AddHostedService<ApprovalConsumerService>();
builder.Services.AddHostedService<ExpiryCheckerService>();

// SignalR
builder.Services.AddSignalR();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:Origins"]?.Split(",") ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApprovalDbContext>();

var app = builder.Build();

// Middleware
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapHub<ApprovalHub>("/hubs/approvals");
app.MapHealthChecks("/health");
app.MapGet("/health/ready", async (ApprovalDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

// Auto-migrate in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApprovalDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

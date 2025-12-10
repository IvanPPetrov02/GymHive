using NotificationsService.BLL.ManagerInterfaces;
using NotificationsService.BLL.Managers;
using NotificationsService.BLL.RepositoryInterfaces;
using NotificationsService.DAL.DbContexts;
using NotificationsService.DAL.Repositories;
using NotificationsService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Prometheus;
using GymHive.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GymHive Notifications API",
        Version = "v1",
        Description = "In-app Notification Service for GymHive"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your JWT token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure MySQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=GymHiveNotifications;User=root;Password=root;";

builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Register Repositories
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Register Managers
builder.Services.AddScoped<INotificationManager, NotificationManager>();

// Configure RabbitMQ Event Bus for subscribing
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
var rabbitMqConnection = $"amqp://{rabbitMqConfig["UserName"]}:{rabbitMqConfig["Password"]}@{rabbitMqConfig["HostName"]}:{rabbitMqConfig["Port"]}{rabbitMqConfig["VirtualHost"]}";
builder.Services.AddRabbitMQEventBus(rabbitMqConnection, "NotificationsService");

// Register Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// Register Event Consumer as Hosted Service
builder.Services.AddHostedService<NotificationEventConsumer>();

// No authentication/authorization middleware needed
// API Gateway handles token validation and adds user headers

var app = builder.Build();

// Run migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    try
    {
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database");
    }
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// Enable Prometheus metrics
app.UseRouting();
app.UseHttpMetrics();

// No authentication/authorization middleware needed
// API Gateway handles token validation and adds user headers

app.MapControllers();

// Prometheus metrics endpoint
app.MapMetrics();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "notifications" }));

app.Run();

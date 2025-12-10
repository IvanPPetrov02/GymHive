// Version: 1.0.2 - RabbitMQ configuration fix
using GymService.BLL.ManagerInterfaces;
using GymService.BLL.Managers;
using GymService.BLL.RepositoryInterfaces;
using GymService.DAL.DbContexts;
using GymService.DAL.Repositories;
using GymService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Prometheus;
using GymHive.Messaging.Interfaces;
using GymHive.Messaging.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GymHive - Gym Service API",
        Version = "v1",
        Description = "API for managing gyms and gym groups"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Just enter your token (without 'Bearer' prefix).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
            Array.Empty<string>()
        }
    });
});

// Register HttpContextAccessor for accessing request headers
builder.Services.AddHttpContextAccessor();

// Register UserContext service to read user info from headers
builder.Services.AddScoped<IUserContextService, UserContextService>();

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<GymDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Register Repositories (using BLL interfaces implemented by DAL)
builder.Services.AddScoped<IGymRepository, GymRepository>();
builder.Services.AddScoped<IGymGroupRepository, GymGroupRepository>();

// Register Managers
builder.Services.AddScoped<IGymManager, GymManager>();
builder.Services.AddScoped<IGymGroupManager, GymGroupManager>();

// Configure RabbitMQ Event Bus
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
var rabbitMqConnection = $"amqp://{rabbitMqConfig["UserName"]}:{rabbitMqConfig["Password"]}@{rabbitMqConfig["HostName"]}:{rabbitMqConfig["Port"]}{rabbitMqConfig["VirtualHost"]}";
builder.Services.AddSingleton<IEventPublisher>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<RabbitMQEventPublisher>>();
    return new RabbitMQEventPublisher(rabbitMqConnection, logger);
});
builder.Services.AddSingleton<IEventSubscriber>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMQEventSubscriber>>();
    return new RabbitMQEventSubscriber(rabbitMqConnection, "gym-service", logger);
});

// Register Background Services
builder.Services.AddHostedService<GymEventConsumer>();

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

var app = builder.Build();

// Run migrations automatically
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<GymDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "GymService",
    timestamp = DateTime.UtcNow
})).AllowAnonymous().ExcludeFromDescription();

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

app.Run();

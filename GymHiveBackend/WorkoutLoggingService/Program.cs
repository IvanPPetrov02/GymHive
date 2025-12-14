// ci: trivial code comment to trigger pipeline
using WorkoutLoggingService.BLL.ManagerInterfaces;
using WorkoutLoggingService.BLL.Managers;
using WorkoutLoggingService.BLL.RepositoryInterfaces;
using WorkoutLoggingService.DAL.DbContexts;
using WorkoutLoggingService.DAL.Repositories;
using WorkoutLoggingService.Services;
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
        Title = "GymHive Workout Logging API",
        Version = "v1",
        Description = "Workout Check-in/Check-out and Logging Service for GymHive"
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
builder.Services.AddDbContext<WorkoutLoggingDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Register Repositories
builder.Services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();

// Register Managers
builder.Services.AddScoped<IWorkoutLogManager, WorkoutLogManager>();

// Configure RabbitMQ Event Bus for publishing
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
var rabbitMqConnection = $"amqp://{rabbitMqConfig["UserName"]}:{rabbitMqConfig["Password"]}@{rabbitMqConfig["HostName"]}:{rabbitMqConfig["Port"]}{rabbitMqConfig["VirtualHost"]}";
builder.Services.AddRabbitMQEventBus(rabbitMqConnection, "WorkoutLoggingService");

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
        var dbContext = services.GetRequiredService<WorkoutLoggingDbContext>();
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
    service = "WorkoutLoggingService",
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


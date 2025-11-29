using WorkoutLoggingService.BLL.ManagerInterfaces;
using WorkoutLoggingService.BLL.Managers;
using WorkoutLoggingService.DAL.DbContexts;
using WorkoutLoggingService.DAL.RepositoryInterfaces;
using WorkoutLoggingService.DAL.Repositories;
using WorkoutLoggingService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
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
    ?? "Server=localhost;Port=3306;Database=GymHiveWorkoutLogs;User=root;Password=root;";

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

// Register Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["AppSettings:Token"] 
    ?? "super_secret_key_that_should_be_changed_in_production_and_be_at_least_32_characters_long!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutLoggingDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Prometheus metrics
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", service = "workout-logging" });

app.Run();

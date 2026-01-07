// Version: 1.0.2 - RabbitMQ configuration fix
// ci: trivial code comment to trigger pipeline
using BLL.ManagerInterfaces;
using BLL;
using BLL.Services;
using BLL.RepositoryInterfaces;
using AuthenticationService.DAL.Repositories;
using DAL.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Prometheus;
using GymHive.Messaging.Interfaces;
using GymHive.Messaging.RabbitMQ;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Data.Common;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GymHive Authentication API",
        Version = "v1",
        Description = "Authentication and User Management API for GymHive"
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

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Register services
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IUserDAO, UserRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

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
    return new RabbitMQEventSubscriber(rabbitMqConnection, "authentication-service", logger);
});

// Register Background Services
builder.Services.AddHostedService<AuthenticationService.Services.AuthEventConsumer>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["AppSettings:Token"] ?? throw new InvalidOperationException("JWT token key not configured");
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await EnsureMigrationsBaselinedAsync(dbContext, logger);
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw;
    }
}

static async Task EnsureMigrationsBaselinedAsync(ApplicationDbContext dbContext, ILogger logger)
{
    var historyRepository = dbContext.GetService<IHistoryRepository>();

    var connection = dbContext.Database.GetDbConnection();
    var shouldCloseConnection = connection.State != ConnectionState.Open;
    if (shouldCloseConnection)
    {
        await connection.OpenAsync();
    }

    try
    {
        var usersTableExists = await TableExistsAsync(connection, "Users");
        if (!usersTableExists)
        {
            return;
        }

        if (!historyRepository.Exists())
        {
            logger.LogInformation("EF migrations history table is missing but Users table exists. Creating history table to baseline migrations.");
            dbContext.Database.ExecuteSqlRaw(historyRepository.GetCreateIfNotExistsScript());
        }

        // If Users exists but there are no applied migrations recorded, we likely previously used EnsureCreated().
        // Baseline the migration history to avoid InitialCreate failing due to existing tables.
        var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (appliedMigrations.Count > 0)
        {
            return;
        }

        logger.LogInformation("No applied migrations recorded. Baselining migration history for existing schema.");

        const string productVersion = "8.0.11";
        InsertMigrationHistory(dbContext, "20251207000000_InitialCreate", productVersion);

        var gymIdExists = await ColumnExistsAsync(connection, "Users", "GymId");
        if (gymIdExists)
        {
            InsertMigrationHistory(dbContext, "20251207201316_AddGymIdToUser", productVersion);
        }
    }
    finally
    {
        if (shouldCloseConnection)
        {
            await connection.CloseAsync();
        }
    }
}

static void InsertMigrationHistory(ApplicationDbContext dbContext, string migrationId, string productVersion)
{
    dbContext.Database.ExecuteSqlRaw(
        "INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ({0}, {1});",
        migrationId,
        productVersion);
}

static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @tableName";

    var parameter = command.CreateParameter();
    parameter.ParameterName = "@tableName";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);

    var result = await command.ExecuteScalarAsync();
    return Convert.ToInt32(result) > 0;
}

static async Task<bool> ColumnExistsAsync(DbConnection connection, string tableName, string columnName)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @tableName AND column_name = @columnName";

    var tableParam = command.CreateParameter();
    tableParam.ParameterName = "@tableName";
    tableParam.Value = tableName;
    command.Parameters.Add(tableParam);

    var columnParam = command.CreateParameter();
    columnParam.ParameterName = "@columnName";
    columnParam.Value = columnName;
    command.Parameters.Add(columnParam);

    var result = await command.ExecuteScalarAsync();
    return Convert.ToInt32(result) > 0;
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// Enable Prometheus metrics
app.UseRouting();
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Prometheus metrics endpoint
app.MapMetrics();

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "AuthenticationService",
    timestamp = DateTime.UtcNow
})).AllowAnonymous().ExcludeFromDescription();

app.Run();

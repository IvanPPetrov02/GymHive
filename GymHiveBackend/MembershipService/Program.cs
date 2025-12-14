// Version: 1.0.2 - RabbitMQ configuration fix
// ci: trivial code comment to trigger pipeline
using MembershipService.BLL.ManagerInterfaces;
using MembershipService.BLL.Managers;
using MembershipService.BLL.RepositoryInterfaces;
using MembershipService.DAL.DbContexts;
using MembershipService.DAL.Repositories;
using MembershipService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Prometheus;
using GymHive.Messaging.Interfaces;
using GymHive.Messaging.RabbitMQ;
using MongoDB.Driver;
using MongoDB.Bson;

// Run migration if --migrate argument is provided
if (args.Length > 0 && args[0] == "--migrate")
{
    await MigrateMemberships();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GymHive Membership API",
        Version = "v1",
        Description = "Membership Management API for GymHive"
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

// Configure MongoDB Database
var connectionString = builder.Configuration.GetValue<string>("MongoDB:ConnectionString") 
    ?? "mongodb://localhost:27017";
var databaseName = builder.Configuration.GetValue<string>("MongoDB:DatabaseName") 
    ?? "GymHiveMemberships";

builder.Services.AddDbContext<MembershipDbContext>(options =>
    options.UseMongoDB(connectionString, databaseName));

// Register Repositories
builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();

// Register Managers
builder.Services.AddScoped<IMembershipManager, MembershipManager>();

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
    return new RabbitMQEventSubscriber(rabbitMqConnection, "membership-service", logger);
});

// Register Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddHttpClient<IGymServiceClient, GymServiceClient>(client =>
{
    var gymServiceUrl = builder.Configuration["GymService:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(gymServiceUrl);
});

// Register Background Services
builder.Services.AddHostedService<MembershipExpirationService>();
builder.Services.AddHostedService<MembershipEventConsumer>();

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

// Run migrations automatically
// MongoDB creates databases and collections automatically - no migration needed

// Configure the HTTP request pipeline
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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "membership" }));

app.Run();

static async Task MigrateMemberships()
{
    Console.WriteLine("Running membership migration...");
    var connectionString = "mongodb://localhost:27017";
    var client = new MongoClient(connectionString);
    var database = client.GetDatabase("GymHiveMembershipsV2");
    var collection = database.GetCollection<BsonDocument>("memberships");

    // Add AutoRenew field to all documents that don't have it
    var filter = Builders<BsonDocument>.Filter.Exists("AutoRenew", false);
    var update = Builders<BsonDocument>.Update.Set("AutoRenew", false);
    
    var result = await collection.UpdateManyAsync(filter, update);
    
    Console.WriteLine($"Migration completed!");
    Console.WriteLine($"Matched: {result.MatchedCount} documents");
    Console.WriteLine($"Modified: {result.ModifiedCount} documents");
}

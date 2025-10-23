using ApiGateway.Services;
using ApiGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add controllers for the documentation page
builder.Services.AddControllers();

// Register AuthService HTTP Client for token introspection
builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
{
    var authServiceUrl = builder.Configuration["AuthService:BaseUrl"] ?? "http://localhost:5010";
    client.BaseAddress = new Uri(authServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",  // Vite default
            "http://localhost:3000",  // Common frontend port
            "http://localhost:4200"   // Angular default
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Serve a simple documentation/landing page
app.MapGet("/", () => Results.Content(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>GymHive API Gateway</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 50px auto; padding: 20px; background: #f5f5f5; }
        .container { background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; border-bottom: 3px solid #4CAF50; padding-bottom: 10px; }
        h2 { color: #666; margin-top: 30px; }
        .service { background: #f9f9f9; padding: 15px; margin: 10px 0; border-radius: 4px; border-left: 4px solid #4CAF50; }
        .service h3 { margin: 0 0 10px 0; color: #333; }
        .service p { margin: 5px 0; color: #666; }
        a { color: #4CAF50; text-decoration: none; font-weight: bold; }
        a:hover { text-decoration: underline; }
        .status { color: #4CAF50; font-weight: bold; }
        .info { background: #e3f2fd; padding: 15px; border-radius: 4px; margin: 20px 0; border-left: 4px solid #2196F3; }
        code { background: #f4f4f4; padding: 2px 6px; border-radius: 3px; font-family: monospace; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🏋️ GymHive API Gateway</h1>
        <p class='status'>✅ Gateway is running and healthy</p>
        
        <div class='info'>
            <strong>ℹ️ Authentication Required:</strong><br>
            All requests through this gateway (except login/register) require a valid JWT token.<br>
            The gateway validates tokens via OAuth 2.0 Token Introspection before routing requests.
        </div>

        <h2>📚 API Documentation</h2>
        <p>Access Swagger UI for each microservice:</p>

        <div class='service'>
            <h3>🔐 Authentication Service</h3>
            <p><strong>Port:</strong> 5010</p>
            <p><strong>Swagger:</strong> <a href='http://localhost:5010/swagger' target='_blank'>http://localhost:5010/swagger</a></p>
            <p><strong>Via Gateway:</strong> <code>http://localhost:5000/api/auth/*</code></p>
            <p>Handles user registration, login, logout, and token introspection</p>
        </div>

        <div class='service'>
            <h3>🏢 Gym Service</h3>
            <p><strong>Port:</strong> 5001</p>
            <p><strong>Swagger:</strong> <a href='http://localhost:5001/swagger' target='_blank'>http://localhost:5001/swagger</a></p>
            <p><strong>Via Gateway:</strong> <code>http://localhost:5000/api/gyms/*</code>, <code>/api/memberships/*</code>, <code>/api/gymgroups/*</code></p>
            <p>Manages gyms, memberships, and gym groups</p>
        </div>

        <h2>🚀 Quick Start</h2>
        <ol>
            <li>Get a token: <code>POST /api/auth/login</code></li>
            <li>Add token to requests: <code>Authorization: Bearer {token}</code></li>
            <li>Access protected resources through the gateway</li>
        </ol>

        <h2>🔍 Health Check</h2>
        <p><a href='/health'>Check Gateway Health</a></p>
    </div>
</body>
</html>
", "text/html")).ExcludeFromDescription();

app.UseCors("AllowFrontend");

// Use token introspection middleware BEFORE routing
app.UseTokenIntrospection();

// Map reverse proxy - this will handle all /api/* routes
app.MapReverseProxy();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    gateway = "GymHive API Gateway",
    version = "1.0.0",
    services = new
    {
        authService = new { url = "http://localhost:5010", status = "check /api/auth/health" },
        gymService = new { url = "http://localhost:5001", status = "check /api/gyms/health" }
    }
})).ExcludeFromDescription();

app.Run();

// Version: 1.0.1 - Country-scale load testing ready
// ci: trivial code comment to trigger pipeline
using ApiGateway.Services;
using ApiGateway.Middleware;
using Prometheus;

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
        // Read allowed origins from environment variable (comma-separated)
        // Default to common development URLs if not set
        var allowedOrigins = builder.Configuration["CorsOrigins"]
            ?? "http://localhost:3000,http://localhost:5173,http://localhost:4200,http://127.0.0.1:3000,http://127.0.0.1:5173";
        
        // Support wildcard for development
        if (allowedOrigins == "*")
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Enable Prometheus metrics
app.UseRouting();
app.UseHttpMetrics();

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
            <p><strong>Via Gateway:</strong> <code>http://localhost:5000/api/gyms/*</code>, <code>/api/gymgroups/*</code></p>
            <p>Manages gyms and gym groups</p>
        </div>

        <div class='service'>
            <h3>👥 Membership Service</h3>
            <p><strong>Port:</strong> 5002</p>
            <p><strong>Swagger:</strong> <a href='http://localhost:5002/swagger' target='_blank'>http://localhost:5002/swagger</a></p>
            <p><strong>Via Gateway:</strong> <code>http://localhost:5000/api/memberships/*</code></p>
            <p>Manages gym memberships and subscriptions</p>
        </div>

        <div class='service'>
            <h3>🔔 Notifications Service</h3>
            <p><strong>Port:</strong> 5003</p>
            <p><strong>Swagger:</strong> <a href='http://localhost:5003/swagger' target='_blank'>http://localhost:5003/swagger</a></p>
            <p><strong>Via Gateway:</strong> <code>http://localhost:5000/api/notifications/*</code></p>
            <p>Manages in-app notifications and event-driven messaging</p>
        </div>

        <div class='service'>
            <h3>💪 Workout Logging Service</h3>
            <p><strong>Port:</strong> 5004</p>
            <p><strong>Swagger:</strong> <a href='http://localhost:5004/swagger' target='_blank'>http://localhost:5004/swagger</a></p>
            <p><strong>Via Gateway:</strong> <code>http://localhost:5000/api/workouts/*</code></p>
            <p>Tracks gym check-ins, check-outs, and workout statistics</p>
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

// Add token introspection middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    
    // Skip authentication for public paths
    // Note: Check BEFORE YARP transformation (original request path)
    if (path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/auth/admin-emails", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/authentication/login", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/authentication/register", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/authentication/admin-emails", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/api/health", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("/health", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
        path == "/")
    {
        await next();
        return;
    }

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var authService = context.RequestServices.GetRequiredService<IAuthServiceClient>();

    // Extract Bearer token
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogWarning("No Bearer token found for path: {Path}", path);
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized: No token provided" });
        return;
    }

    var token = authHeader.Substring("Bearer ".Length).Trim();

    // Validate token
    try
    {
        var result = await authService.IntrospectTokenAsync(token);

        if (!result.Active)
        {
            logger.LogWarning("Invalid token for path: {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized: Invalid token" });
            return;
        }

        // Add user context headers - YARP will forward these
        context.Request.Headers["X-User-Id"] = result.UserId ?? "";
        context.Request.Headers["X-User-Email"] = result.Email ?? "";
        context.Request.Headers["X-User-Role"] = result.Role ?? "";
        context.Request.Headers["X-User-GymId"] = result.GymId?.ToString() ?? "";

        logger.LogInformation("✅ Token validated (Role: {Role}) for {Path}", result.Role, path);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during token introspection for path: {Path}", path);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error during authentication" });
        return;
    }

    await next();
});

// Map reverse proxy
app.MapReverseProxy();

// Prometheus metrics endpoint
app.MapMetrics();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    gateway = "GymHive API Gateway",
    version = "1.0.0",
    services = new
    {
        authService = new { url = "http://localhost:5010", status = "check /api/auth/health" },
        gymService = new { url = "http://localhost:5001", status = "check /api/gyms/health" },
        membershipService = new { url = "http://localhost:5002", status = "check /api/memberships/health" },
        notificationsService = new { url = "http://localhost:5003", status = "check /api/notifications/health" },
        workoutsService = new { url = "http://localhost:5004", status = "check /api/workouts/health" }
    }
})).ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    gateway = "GymHive API Gateway",
    version = "1.0.0",
    services = new
    {
        authService = new { url = "http://localhost:5010", status = "check /api/auth/health" },
        gymService = new { url = "http://localhost:5001", status = "check /api/gyms/health" },
        membershipService = new { url = "http://localhost:5002", status = "check /api/memberships/health" },
        notificationsService = new { url = "http://localhost:5003", status = "check /api/notifications/health" },
        workoutsService = new { url = "http://localhost:5004", status = "check /api/workouts/health" }
    }
})).ExcludeFromDescription();

app.Run();

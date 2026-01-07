using ApiGateway.Services;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware that validates JWT tokens by calling AuthService introspection endpoint
/// and adds user information to request headers for downstream services
/// </summary>
public class TokenIntrospectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenIntrospectionMiddleware> _logger;

    public TokenIntrospectionMiddleware(RequestDelegate next, ILogger<TokenIntrospectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthServiceClient authServiceClient)
    {
        // Skip authentication for certain paths
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        
        if (ShouldSkipAuthentication(path))
        {
            _logger.LogDebug("Skipping authentication for path: {Path}", path);
            await _next(context);
            return;
        }

        // Extract token from Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning("Missing Authorization header for path: {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing Authorization header" });
            return;
        }

        // Remove "Bearer " prefix
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader.Substring(7)
            : authHeader;

        // Validate token via introspection
        var validationResult = await authServiceClient.IntrospectTokenAsync(token);

        if (!validationResult.Active)
        {
            _logger.LogWarning("Invalid token for path: {Path}, Error: {Error}", path, validationResult.Error);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = $"Bearer error=\"invalid_token\", error_description=\"{validationResult.Error}\"";
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token", details = validationResult.Error });
            return;
        }

        // Add user information to request headers for downstream services
        context.Request.Headers["X-User-Id"] = validationResult.UserId ?? string.Empty;
        context.Request.Headers["X-User-Email"] = validationResult.Email ?? string.Empty;
        context.Request.Headers["X-User-Role"] = validationResult.Role ?? string.Empty;

        _logger.LogInformation(
            "Authenticated request for path: {Path}, Role: {Role}",
            path, validationResult.Role
        );

        // Continue to next middleware
        await _next(context);
    }

    /// <summary>
    /// Determines if authentication should be skipped for the given path
    /// </summary>
    private bool ShouldSkipAuthentication(string path)
    {
        var publicPaths = new[]
        {
            "/",  // Landing page
            "/health",
            "/swagger",
            "/favicon.ico",
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/admin-emails",
            "/api/authentication/register",
            "/api/authentication/login",
            "/api/authentication/admin-emails",
            "/api/authentication/introspect" // Allow introspection endpoint itself
        };

        // Check exact match for root path
        if (path == "/" || string.IsNullOrEmpty(path))
            return true;

        return publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class TokenIntrospectionMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenIntrospection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenIntrospectionMiddleware>();
    }
}

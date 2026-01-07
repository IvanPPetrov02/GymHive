using System.Text;
using System.Text.Json;

namespace ApiGateway.Services;

/// <summary>
/// HTTP client for communicating with the Authentication Service
/// Implements OAuth 2.0 Token Introspection (RFC 7662)
/// </summary>
public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;
    private readonly string _introspectionEndpoint;

    public AuthServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        var authServiceUrl = configuration["AuthService:BaseUrl"] ?? "http://localhost:5010";
        _introspectionEndpoint = $"{authServiceUrl}/api/Authentication/introspect";
        
        _logger.LogInformation("AuthServiceClient initialized with endpoint: {Endpoint}", _introspectionEndpoint);
    }

    public async Task<TokenValidationResponseDTO> IntrospectTokenAsync(string token)
    {
        try
        {
            var request = new TokenValidationRequestDTO { Token = token };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Calling introspection endpoint: {Endpoint}", _introspectionEndpoint);
            
            var response = await _httpClient.PostAsync(_introspectionEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Introspection endpoint returned {StatusCode}", response.StatusCode);
                return new TokenValidationResponseDTO
                {
                    Active = false,
                    Error = $"AuthService returned {response.StatusCode}"
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TokenValidationResponseDTO>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                _logger.LogError("Failed to deserialize introspection response");
                return new TokenValidationResponseDTO
                {
                    Active = false,
                    Error = "Invalid response from AuthService"
                };
            }

            _logger.LogInformation("Token validation result: Active={Active}, Role={Role}",
                result.Active, result.Role);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling AuthService introspection endpoint");
            return new TokenValidationResponseDTO
            {
                Active = false,
                Error = "AuthService unavailable"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token introspection");
            return new TokenValidationResponseDTO
            {
                Active = false,
                Error = "Token validation failed"
            };
        }
    }
}

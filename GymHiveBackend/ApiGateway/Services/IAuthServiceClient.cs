namespace ApiGateway.Services;

/// <summary>
/// Client for communicating with the Authentication Service
/// </summary>
public interface IAuthServiceClient
{
    /// <summary>
    /// Validates a JWT token by calling the AuthService introspection endpoint
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>Token validation response with user information</returns>
    Task<TokenValidationResponseDTO> IntrospectTokenAsync(string token);
}

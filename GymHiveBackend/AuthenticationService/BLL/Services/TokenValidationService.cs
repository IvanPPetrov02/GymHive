using BLL.DTOs;
using BLL.ManagerInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BLL.Services;

public class TokenValidationService : ITokenValidationService
{
    private readonly string _secretKey;
    private readonly IUserManager _userManager;

    public TokenValidationService(IConfiguration configuration, IUserManager userManager)
    {
        _secretKey = configuration["AppSettings:Token"] ??
                     throw new InvalidOperationException("JWT secret key must be set.");
        _userManager = userManager;
    }

    public async Task<TokenValidationResponseDTO> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            // Validate token signature and claims
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false, // We control the issuer
                ValidateAudience = false, // Multiple audiences possible
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Extract claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return new TokenValidationResponseDTO
                {
                    Active = false,
                    Error = "Invalid user ID in token"
                };
            }

            // Verify user still exists and is active in database
            var user = await _userManager.GetLoggedUserAsync(userIdClaim);
            if (user == null)
            {
                return new TokenValidationResponseDTO
                {
                    Active = false,
                    Error = "User not found"
                };
            }

            if (!user.IsActive)
            {
                return new TokenValidationResponseDTO
                {
                    Active = false,
                    Error = "User account is inactive"
                };
            }

            // Get expiration time
            var exp = principal.FindFirst("exp")?.Value;
            long? expiration = null;
            if (!string.IsNullOrEmpty(exp) && long.TryParse(exp, out var expValue))
            {
                expiration = expValue;
            }

            return new TokenValidationResponseDTO
            {
                Active = true,
                UserId = userId,
                Email = emailClaim,
                Role = roleClaim,
                Exp = expiration
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResponseDTO
            {
                Active = false,
                Error = "Token has expired"
            };
        }
        catch (SecurityTokenException ex)
        {
            return new TokenValidationResponseDTO
            {
                Active = false,
                Error = $"Invalid token: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new TokenValidationResponseDTO
            {
                Active = false,
                Error = $"Token validation failed: {ex.Message}"
            };
        }
    }
}

using BLL.DTOs;

namespace BLL.Services;

public interface ITokenValidationService
{
    Task<TokenValidationResponseDTO> ValidateTokenAsync(string token);
}

namespace BLL.DTOs;

public class TokenValidationRequestDTO
{
    public required string Token { get; set; }
}

public class TokenValidationResponseDTO
{
    public bool Active { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public long? Exp { get; set; }
    public string? Error { get; set; }
}

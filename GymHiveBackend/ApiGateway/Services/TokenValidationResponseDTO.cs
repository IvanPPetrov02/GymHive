namespace ApiGateway.Services;

public class TokenValidationResponseDTO
{
    public bool Active { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public int? GymId { get; set; }
    public long? Exp { get; set; }
    public string? Error { get; set; }
}

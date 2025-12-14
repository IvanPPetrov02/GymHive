using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs;

public class UserDTO
{
    public required string UUID { get; set; }
    
    [EmailAddress]
    public required string Email { get; set; }
    
    public required string Name { get; set; }
    
    public required string Surname { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public string Role { get; set; } = "User";
    
    public int? GymId { get; set; }
}
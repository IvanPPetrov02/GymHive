using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs;

public class UserRegisterDTO
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email is not valid")]
    [MaxLength(50)]
    public required string Email { get; set; }
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [MaxLength(90)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?"":{}|<>+-]).{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character")]
    public required string Password { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(50)]
    public required string Name { get; set; }
    
    [Required(ErrorMessage = "Surname is required")]
    [MaxLength(50)]
    public required string Surname { get; set; }
}
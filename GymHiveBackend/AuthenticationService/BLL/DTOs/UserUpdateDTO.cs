using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs;

public class UserUpdateDTO
{
    [EmailAddress(ErrorMessage = "Email is not valid")]
    [MaxLength(50)]
    public string? Email { get; set; }
    
    [MaxLength(50)]
    public string? Name { get; set; }
    
    [MaxLength(50)]
    public string? Surname { get; set; }
}
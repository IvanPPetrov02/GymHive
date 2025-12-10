namespace BLL.DTOs;

public class CreateModeratorDTO
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string GymName { get; set; } = string.Empty;
    public int GymId { get; set; }
}

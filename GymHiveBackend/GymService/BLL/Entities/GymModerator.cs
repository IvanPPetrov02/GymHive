namespace GymService.BLL.Entities;

/// <summary>
/// Junction table to link moderators (users) to gyms they can manage
/// </summary>
public class GymModerator
{
    public int Id { get; set; }
    public int GymId { get; set; }
    public Guid ModeratorUserId { get; set; } // UUID from AuthenticationService
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Gym Gym { get; set; } = null!;
}

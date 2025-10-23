namespace GymService.BLL.Entities;

public class GymGroup
{
    public int Id { get; set; }
    public int GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ModeratorId { get; set; } // Foreign key to User (moderator)
    public int MaxMembers { get; set; }
    public string Schedule { get; set; } = string.Empty; // e.g., "Monday 18:00-20:00"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    public Gym Gym { get; set; } = null!;
}

namespace GymService.BLL.Entities;

public class GymGroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public GymGroup Group { get; set; } = null!;
}

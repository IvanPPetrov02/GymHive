namespace NotificationsService.BLL.Entities;

public class Notification
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty; // MembershipPurchased, ClassBooked, WorkoutReminder, NewPost, FriendRequest, Message
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedEntityId { get; set; } // gym ID, class ID, post ID, etc.
    public string? RelatedEntityType { get; set; } // "gym", "class", "post", "booking", "friend", "message"
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

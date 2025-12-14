namespace GymHive.Messaging.Events;

public class PostCreatedEvent : BaseEvent
{
    public override string EventType => "PostCreated";
    
    public int PostId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? WorkoutId { get; set; }
    public List<string> MediaUrls { get; set; } = new();
}

public class PostLikedEvent : BaseEvent
{
    public override string EventType => "PostLiked";
    
    public int PostId { get; set; }
    public Guid LikedBy { get; set; }
    public Guid PostAuthorId { get; set; }
}

public class CommentAddedEvent : BaseEvent
{
    public override string EventType => "CommentAdded";
    
    public int CommentId { get; set; }
    public int PostId { get; set; }
    public Guid UserId { get; set; }
    public Guid PostAuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class PostDeletedEvent : BaseEvent
{
    public override string EventType => "PostDeleted";
    
    public int PostId { get; set; }
    public Guid UserId { get; set; }
}

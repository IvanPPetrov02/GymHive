namespace GymHive.Messaging.Events;

public class GymCreatedEvent : BaseEvent
{
    public override string EventType => "GymCreated";
    
    public int GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
}

public class GymUpdatedEvent : BaseEvent
{
    public override string EventType => "GymUpdated";
    
    public int GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int UpdatedBy { get; set; }
}

public class GymDeletedEvent : BaseEvent
{
    public override string EventType => "GymDeleted";
    
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public int DeletedBy { get; set; }
    public string? SagaId { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Command to demote gym moderators to regular users when gym is deleted
/// </summary>
public class DemoteModeratorsCommand : BaseEvent
{
    public override string EventType => "DemoteModerators";
    
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public List<int> ModeratorIds { get; set; } = new();
}

public class ModeratorsPromotedEvent : BaseEvent
{
    public override string EventType => "ModeratorsPromoted";
    
    public int GymId { get; set; }
    public List<int> ModeratorIds { get; set; } = new();
    public DateTime PromotedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Command to create moderator users for a gym
/// </summary>
public class CreateModeratorsCommand : BaseEvent
{
    public override string EventType => "CreateModerators";
    
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public List<ModeratorInfo> Moderators { get; set; } = new();
}

public class ModeratorInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class ModeratorsCreatedEvent : BaseEvent
{
    public override string EventType => "ModeratorsCreated";
    
    public int GymId { get; set; }
    public List<CreatedModeratorInfo> Moderators { get; set; } = new();
}

public class CreatedModeratorInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class GymGroupMemberAddedEvent : BaseEvent
{
    public override string EventType => "GymGroupMemberAdded";
    
    public int GymGroupId { get; set; }
    public string GymGroupName { get; set; } = string.Empty;
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid AddedBy { get; set; }
}

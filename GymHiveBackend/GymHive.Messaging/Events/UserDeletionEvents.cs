namespace GymHive.Messaging.Events;

// ==================== USER DELETION SAGA (Choreographed) ====================
// Flow: UserDeletionInitiated → MembershipsDeleted → NotificationsDeleted → WorkoutLogsDeleted → UserDeleted

/// <summary>
/// Initiates user deletion SAGA across all services
/// </summary>
public class UserDeletionInitiatedEvent : BaseEvent
{
    public override string EventType => "UserDeletionInitiated";
    
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SagaId { get; set; } = string.Empty;
}

/// <summary>
/// Published by MembershipService after deleting user's memberships
/// </summary>
public class UserMembershipsDeletedEvent : BaseEvent
{
    public override string EventType => "UserMembershipsDeleted";
    
    public Guid UserId { get; set; }
    public string SagaId { get; set; } = string.Empty;
    public int DeletedCount { get; set; }
    public List<string> DeletedMembershipIds { get; set; } = new();
}

/// <summary>
/// Published by NotificationsService after deleting user's notifications
/// </summary>
public class UserNotificationsDeletedEvent : BaseEvent
{
    public override string EventType => "UserNotificationsDeleted";
    
    public Guid UserId { get; set; }
    public string SagaId { get; set; } = string.Empty;
    public int DeletedCount { get; set; }
}

/// <summary>
/// Published by WorkoutLoggingService after deleting user's workout logs
/// </summary>
public class UserWorkoutLogsDeletedEvent : BaseEvent
{
    public override string EventType => "UserWorkoutLogsDeleted";
    
    public Guid UserId { get; set; }
    public string SagaId { get; set; } = string.Empty;
    public int DeletedCount { get; set; }
}

/// <summary>
/// Final event - user successfully deleted from all services
/// </summary>
public class UserDeletedEvent : BaseEvent
{
    public override string EventType => "UserDeleted";
    
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SagaId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}

/// <summary>
/// Compensation event - rollback user deletion if any step fails
/// </summary>
public class UserDeletionFailedEvent : BaseEvent
{
    public override string EventType => "UserDeletionFailed";
    
    public Guid UserId { get; set; }
    public string SagaId { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public string FailedService { get; set; } = string.Empty;
}

// ==================== MEMBERSHIP PURCHASE SAGA (Choreographed) ====================
// Flow: MembershipPurchased → AddToGymGroup → MembershipActivated

/// <summary>
/// Published when membership is successfully created (already exists)
/// Now will trigger gym group addition
/// </summary>
public class MembershipActivatedEvent : BaseEvent
{
    public override string EventType => "MembershipActivated";
    
    public string MembershipId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public string SagaId { get; set; } = string.Empty;
}

/// <summary>
/// Compensation - rollback membership if gym group addition fails
/// </summary>
public class MembershipPurchaseFailedEvent : BaseEvent
{
    public override string EventType => "MembershipPurchaseFailed";
    
    public string MembershipId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public string SagaId { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
}

// ==================== GYM DELETION SAGA (Choreographed) ====================
// Flow: GymDeletionInitiated → MembershipsDeleted → GroupDeleted → ModeratorsUpdated → GymDeleted

/// <summary>
/// Initiates gym deletion SAGA
/// </summary>
public class GymDeletionInitiatedEvent : BaseEvent
{
    public override string EventType => "GymDeletionInitiated";
    
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public string SagaId { get; set; } = string.Empty;
    public List<Guid> ModeratorIds { get; set; } = new();
}

/// <summary>
/// Published by MembershipService after deleting all gym memberships
/// </summary>
public class GymMembershipsDeletedEvent : BaseEvent
{
    public override string EventType => "GymMembershipsDeleted";
    
    public int GymId { get; set; }
    public string SagaId { get; set; } = string.Empty;
    public int DeletedCount { get; set; }
}

/// <summary>
/// Published by GymService after deleting gym group
/// </summary>
public class GymGroupDeletedEvent : BaseEvent
{
    public override string EventType => "GymGroupDeleted";
    
    public int GymId { get; set; }
    public int GroupId { get; set; }
    public string SagaId { get; set; } = string.Empty;
}

/// <summary>
/// Published by AuthService after demoting moderators to regular users
/// </summary>
public class GymModeratorsUpdatedEvent : BaseEvent
{
    public override string EventType => "GymModeratorsUpdated";
    
    public int GymId { get; set; }
    public string SagaId { get; set; } = string.Empty;
    public List<Guid> UpdatedUserIds { get; set; } = new();
}

/// <summary>
/// Compensation event - rollback gym deletion
/// </summary>
public class GymDeletionFailedEvent : BaseEvent
{
    public override string EventType => "GymDeletionFailed";
    
    public int GymId { get; set; }
    public string SagaId { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public string FailedService { get; set; } = string.Empty;
}

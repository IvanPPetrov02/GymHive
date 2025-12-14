namespace GymHive.Messaging.Events;

/// <summary>
/// Command to validate if a gym has capacity for a new member
/// </summary>
public class ValidateGymCapacityCommand : BaseEvent
{
    public override string EventType => "ValidateGymCapacity";
    
    public int GymId { get; set; }
    public Guid UserId { get; set; }
    public string SagaId { get; set; } = string.Empty;
}

/// <summary>
/// Response from gym capacity validation
/// </summary>
public class GymCapacityValidatedEvent : BaseEvent
{
    public override string EventType => "GymCapacityValidated";
    
    public string SagaId { get; set; } = string.Empty;
    public int GymId { get; set; }
    public bool HasCapacity { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Command to update gym member count
/// </summary>
public class UpdateGymMemberCountCommand : BaseEvent
{
    public override string EventType => "UpdateGymMemberCount";
    
    public int GymId { get; set; }
    public int Delta { get; set; } // +1 for add, -1 for remove
    public string SagaId { get; set; } = string.Empty;
}

/// <summary>
/// Confirmation that gym member count was updated
/// </summary>
public class GymMemberCountUpdatedEvent : BaseEvent
{
    public override string EventType => "GymMemberCountUpdated";
    
    public string SagaId { get; set; } = string.Empty;
    public int GymId { get; set; }
    public int NewMemberCount { get; set; }
}

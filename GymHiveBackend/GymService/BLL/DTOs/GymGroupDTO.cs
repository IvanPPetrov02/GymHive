namespace GymService.BLL.DTOs;

public class GymGroupDTO
{
    public int Id { get; set; }
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ModeratorId { get; set; }
    public int MaxMembers { get; set; }
    public string Schedule { get; set; } = string.Empty;
}

public class CreateGymGroupDTO
{
    public int GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ModeratorId { get; set; }
    public int MaxMembers { get; set; }
    public string Schedule { get; set; } = string.Empty;
}

public class UpdateGymGroupDTO
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? MaxMembers { get; set; }
    public string? Schedule { get; set; }
}

public class GymGroupMemberDTO
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

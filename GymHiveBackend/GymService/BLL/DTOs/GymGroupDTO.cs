namespace GymService.BLL.DTOs;

public class GymGroupDTO
{
    public int Id { get; set; }
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ModeratorId { get; set; }
    public int MaxMembers { get; set; }
    public string Schedule { get; set; } = string.Empty;
}

public class CreateGymGroupDTO
{
    public int GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ModeratorId { get; set; }
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

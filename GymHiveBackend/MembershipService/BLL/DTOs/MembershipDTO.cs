namespace MembershipService.BLL.DTOs;

public class MembershipDTO
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public string MembershipType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public decimal Price { get; set; }
}

public class CreateMembershipDTO
{
    // UserId will be extracted from JWT token, not from request body
    public int GymId { get; set; }
    public string MembershipType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
}

public class UpdateMembershipDTO
{
    public string? MembershipType { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsActive { get; set; }
}

public class GymDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

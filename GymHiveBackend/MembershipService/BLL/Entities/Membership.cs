using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MembershipService.BLL.Entities;

public class Membership
{
    [Key]
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int Id { get; set; }
    public Guid UserId { get; set; } // Foreign key to User UUID from AuthenticationService
    public int GymId { get; set; } // Foreign key to Gym from GymService
    public string MembershipType { get; set; } = string.Empty; // e.g., "Basic", "Premium", "VIP"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using GymService.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembershipsController : ControllerBase
{
    private readonly IMembershipManager _membershipManager;
    private readonly IUserContextService _userContext;

    public MembershipsController(IMembershipManager membershipManager, IUserContextService userContext)
    {
        _membershipManager = membershipManager;
        _userContext = userContext;
    }

    // Admin only - View all memberships
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MembershipDTO>>> GetAllMemberships()
    {
        // Check if user is Admin
        if (!_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        var memberships = await _membershipManager.GetAllMembershipsAsync();
        return Ok(memberships);
    }

    // User can view their own, Admin can view any
    [HttpGet("{id}")]
    public async Task<ActionResult<MembershipDTO>> GetMembershipById(int id)
    {
        var membership = await _membershipManager.GetMembershipByIdAsync(id);
        if (membership == null) return NotFound();
        return Ok(membership);
    }

    // User can view their own memberships, Admin can view any user's
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<MembershipDTO>>> GetMembershipsByUserId(Guid userId)
    {
        var memberships = await _membershipManager.GetMembershipsByUserIdAsync(userId);
        return Ok(memberships);
    }

    // User can view their own memberships
    [HttpGet("my-memberships")]
    public async Task<ActionResult<IEnumerable<MembershipDTO>>> GetMyMemberships()
    {
        // Get user ID from headers (set by API Gateway)
        var userId = _userContext.GetCurrentUserId();
        var memberships = await _membershipManager.GetMembershipsByUserIdAsync(userId);
        return Ok(memberships);
    }

    // Admin and Moderator can view gym memberships
    [HttpGet("gym/{gymId}")]
    public async Task<ActionResult<IEnumerable<MembershipDTO>>> GetMembershipsByGymId(int gymId)
    {
        // Check if user is Admin or Moderator
        var role = _userContext.GetCurrentUserRole();
        if (role != "Admin" && role != "Moderator")
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        var memberships = await _membershipManager.GetMembershipsByGymIdAsync(gymId);
        return Ok(memberships);
    }

    // User can create their own membership
    [HttpPost]
    public async Task<ActionResult<MembershipDTO>> CreateMembership([FromBody] CreateMembershipDTO createMembershipDto)
    {
        // Get user ID from headers
        var userId = _userContext.GetCurrentUserId();
        var membership = await _membershipManager.CreateMembershipAsync(userId, createMembershipDto);
        return CreatedAtAction(nameof(GetMembershipById), new { id = membership.Id }, membership);
    }

    // User can update their own membership, Admin can update any
    [HttpPut("{id}")]
    public async Task<ActionResult<MembershipDTO>> UpdateMembership(int id, [FromBody] UpdateMembershipDTO updateMembershipDto)
    {
        var membership = await _membershipManager.UpdateMembershipAsync(id, updateMembershipDto);
        if (membership == null) return NotFound();
        return Ok(membership);
    }

    // Admin only - Delete membership
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMembership(int id)
    {
        // Check if user is Admin
        if (!_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        var result = await _membershipManager.DeleteMembershipAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}

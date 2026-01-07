using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using GymService.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GymGroupsController : ControllerBase
{
    private readonly IGymGroupManager _gymGroupManager;
    private readonly IUserContextService _userContext;
    private readonly ILogger<GymGroupsController> _logger;

    public GymGroupsController(IGymGroupManager gymGroupManager, IUserContextService userContext, ILogger<GymGroupsController> logger)
    {
        _gymGroupManager = gymGroupManager;
        _userContext = userContext;
        _logger = logger;
    }

    // Public - Anyone can view all groups
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GymGroupDTO>>> GetAllGymGroups()
    {
        var gymGroups = await _gymGroupManager.GetAllGymGroupsAsync();
        return Ok(gymGroups);
    }

    // Public - Anyone can view a specific group
    [HttpGet("{id}")]
    public async Task<ActionResult<GymGroupDTO>> GetGymGroupById(int id)
    {
        var gymGroup = await _gymGroupManager.GetGymGroupByIdAsync(id);
        if (gymGroup == null) return NotFound();
        return Ok(gymGroup);
    }

    // Public - Anyone can view groups by gym
    [HttpGet("gym/{gymId}")]
    public async Task<ActionResult<IEnumerable<GymGroupDTO>>> GetGymGroupsByGymId(int gymId)
    {
        var gymGroups = await _gymGroupManager.GetGymGroupsByGymIdAsync(gymId);
        return Ok(gymGroups);
    }

    // Moderator can view their own groups, Admin can view any
    [HttpGet("moderator/{moderatorId}")]
    public async Task<ActionResult<IEnumerable<GymGroupDTO>>> GetGymGroupsByModeratorId(Guid moderatorId)
    {
        // Check if user is Moderator or Admin
        if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        if (!_userContext.IsInRole("Admin"))
        {
            try
            {
                var currentUserId = _userContext.GetCurrentUserId();
                if (moderatorId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden" });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(401, new { error = "Unauthorized" });
            }
        }

        var gymGroups = await _gymGroupManager.GetGymGroupsByModeratorIdAsync(moderatorId);
        return Ok(gymGroups);
    }

    // Moderator views groups for their gym, Admin can view all
    [HttpGet("my-moderated")]
    public async Task<ActionResult<IEnumerable<GymGroupDTO>>> GetMyModeratedGroups()
    {
        try
        {
            // Check if user is Moderator or Admin
            if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
            {
                return StatusCode(403, new { error = "Forbidden" });
            }

            // Get moderator's gymId from headers
            var gymId = _userContext.GetCurrentUserGymId();
            _logger.LogInformation("GetMyModeratedGroups - GymId from headers: {GymId}", gymId);
            
            if (!gymId.HasValue)
            {
                _logger.LogWarning("GetMyModeratedGroups - No GymId found in headers");
                return BadRequest(new { error = "Moderator must have a gym assigned" });
            }

            // Return all groups for the moderator's gym
            var gymGroups = await _gymGroupManager.GetGymGroupsByGymIdAsync(gymId.Value);
            _logger.LogInformation("GetMyModeratedGroups - Found {Count} groups for gym {GymId}", gymGroups.Count(), gymId);
            return Ok(gymGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMyModeratedGroups: {Message}", ex.Message);
            return StatusCode(500, new { error = "Failed to get moderated groups", details = ex.Message });
        }
    }

    // Moderator can create groups for their gym, Admin can create any
    [HttpPost]
    public async Task<ActionResult<GymGroupDTO>> CreateGymGroup([FromBody] CreateGymGroupDTO createGymGroupDto)
    {
        try
        {
            _logger.LogInformation("CreateGymGroup called - GymId: {GymId}, Name: {Name}, ModeratorId: {ModeratorId}", 
                createGymGroupDto.GymId, createGymGroupDto.Name, createGymGroupDto.ModeratorId);

            // Check if user is Moderator or Admin
            if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
            {
                _logger.LogWarning("CreateGymGroup - Access denied, user is not Moderator or Admin");
                return StatusCode(403, new { error = "Forbidden" });
            }

            if (!_userContext.IsInRole("Admin"))
            {
                var currentGymId = _userContext.GetCurrentUserGymId();
                if (!currentGymId.HasValue)
                {
                    return BadRequest(new { error = "Moderator must have a gym assigned" });
                }

                var currentUserId = _userContext.GetCurrentUserId();

                // Prevent moderators from creating groups for other gyms/users
                createGymGroupDto.GymId = currentGymId.Value;
                createGymGroupDto.ModeratorId = currentUserId;
            }

            _logger.LogInformation("CreateGymGroup - Role check passed, creating gym group...");
            var gymGroup = await _gymGroupManager.CreateGymGroupAsync(createGymGroupDto);
            _logger.LogInformation("CreateGymGroup - Successfully created gym group with ID: {Id}", gymGroup.Id);
            return CreatedAtAction(nameof(GetGymGroupById), new { id = gymGroup.Id }, gymGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gym group: {Message}. StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            return StatusCode(500, new { error = "Failed to create gym group", details = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    // Moderator can update their own groups, Admin can update any
    [HttpPut("{id}")]
    public async Task<ActionResult<GymGroupDTO>> UpdateGymGroup(int id, [FromBody] UpdateGymGroupDTO updateGymGroupDto)
    {
        // Check if user is Moderator or Admin
        if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        if (!_userContext.IsInRole("Admin"))
        {
            try
            {
                var currentUserId = _userContext.GetCurrentUserId();
                var existing = await _gymGroupManager.GetGymGroupByIdAsync(id);
                if (existing == null) return NotFound();
                if (existing.ModeratorId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden" });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(401, new { error = "Unauthorized" });
            }
        }

        var gymGroup = await _gymGroupManager.UpdateGymGroupAsync(id, updateGymGroupDto);
        if (gymGroup == null) return NotFound();
        return Ok(gymGroup);
    }

    // Moderator can delete their own groups, Admin can delete any
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGymGroup(int id)
    {
        // Check if user is Moderator or Admin
        if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        if (!_userContext.IsInRole("Admin"))
        {
            try
            {
                var currentUserId = _userContext.GetCurrentUserId();
                var existing = await _gymGroupManager.GetGymGroupByIdAsync(id);
                if (existing == null) return NotFound();
                if (existing.ModeratorId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden" });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(401, new { error = "Unauthorized" });
            }
        }

        var result = await _gymGroupManager.DeleteGymGroupAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinGymGroup(int id, [FromBody] JoinGroupRequest request)
    {
        try
        {
            if (!_userContext.IsInRole("Admin"))
            {
                var currentUserId = _userContext.GetCurrentUserId();
                if (request.UserId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden" });
                }
            }

            await _gymGroupManager.AddMemberAsync(id, request.UserId);
            return Ok(new { message = "Successfully joined the group" });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(401, new { error = "Unauthorized" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveGymGroup(int id, [FromBody] LeaveGroupRequest request)
    {
        try
        {
            if (!_userContext.IsInRole("Admin"))
            {
                var currentUserId = _userContext.GetCurrentUserId();
                if (request.UserId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden" });
                }
            }

            await _gymGroupManager.RemoveMemberByUserIdAsync(id, request.UserId);
            return Ok(new { message = "Successfully left the group" });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(401, new { error = "Unauthorized" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<IEnumerable<GymGroupMemberDTO>>> GetGroupMembers(int id)
    {
        if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        if (!_userContext.IsInRole("Admin"))
        {
            try
            {
                var currentUserId = _userContext.GetCurrentUserId();
                var existing = await _gymGroupManager.GetGymGroupByIdAsync(id);
                if (existing == null) return NotFound();
                if (existing.ModeratorId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden" });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(401, new { error = "Unauthorized" });
            }
        }

        var members = await _gymGroupManager.GetGroupMembersAsync(id);
        return Ok(members);
    }

    [HttpDelete("{groupId}/members/{userId}")]
    public async Task<IActionResult> RemoveMemberFromGroup(int groupId, string userId)
    {
        if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        try
        {
            if (!_userContext.IsInRole("Admin"))
            {
                var currentUserId = _userContext.GetCurrentUserId();
                var existing = await _gymGroupManager.GetGymGroupByIdAsync(groupId);
                if (existing == null) return NotFound();
                if (existing.ModeratorId != currentUserId)
                {
                    return StatusCode(403, new { error = "Forbidden" });
                }
            }

            Guid userGuid;
            try
            {
                userGuid = new Guid(userId);
            }
            catch
            {
                return BadRequest("Invalid user ID format");
            }

            await _gymGroupManager.RemoveMemberByUserIdAsync(groupId, userGuid);
            return Ok(new { message = "Member removed successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(401, new { error = "Unauthorized" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

}

public record JoinGroupRequest(Guid UserId);
public record LeaveGroupRequest(Guid UserId);

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

    public GymGroupsController(IGymGroupManager gymGroupManager, IUserContextService userContext)
    {
        _gymGroupManager = gymGroupManager;
        _userContext = userContext;
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
    public async Task<ActionResult<IEnumerable<GymGroupDTO>>> GetGymGroupsByModeratorId(int moderatorId)
    {
        // Check if user is Moderator or Admin
        if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        var gymGroups = await _gymGroupManager.GetGymGroupsByModeratorIdAsync(moderatorId);
        return Ok(gymGroups);
    }

    // Moderator can create groups for their gym, Admin can create any
    [HttpPost]
    public async Task<ActionResult<GymGroupDTO>> CreateGymGroup([FromBody] CreateGymGroupDTO createGymGroupDto)
    {
        // Check if user is Moderator or Admin
        if (!_userContext.IsInRole("Moderator") && !_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden" });
        }

        var gymGroup = await _gymGroupManager.CreateGymGroupAsync(createGymGroupDto);
        return CreatedAtAction(nameof(GetGymGroupById), new { id = gymGroup.Id }, gymGroup);
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

        var result = await _gymGroupManager.DeleteGymGroupAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}

using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GymGroupsController : ControllerBase
{
    private readonly IGymGroupManager _gymGroupManager;

    public GymGroupsController(IGymGroupManager gymGroupManager)
    {
        _gymGroupManager = gymGroupManager;
    }

    // Public - Anyone can view all groups
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GymGroupDTO>>> GetAllGymGroups()
    {
        var gymGroups = await _gymGroupManager.GetAllGymGroupsAsync();
        return Ok(gymGroups);
    }

    // Public - Anyone can view a specific group
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<GymGroupDTO>> GetGymGroupById(int id)
    {
        var gymGroup = await _gymGroupManager.GetGymGroupByIdAsync(id);
        if (gymGroup == null) return NotFound();
        return Ok(gymGroup);
    }

    // Public - Anyone can view groups by gym
    [HttpGet("gym/{gymId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GymGroupDTO>>> GetGymGroupsByGymId(int gymId)
    {
        var gymGroups = await _gymGroupManager.GetGymGroupsByGymIdAsync(gymId);
        return Ok(gymGroups);
    }

    // Moderator can view their own groups, Admin can view any
    [HttpGet("moderator/{moderatorId}")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<ActionResult<IEnumerable<GymGroupDTO>>> GetGymGroupsByModeratorId(int moderatorId)
    {
        var gymGroups = await _gymGroupManager.GetGymGroupsByModeratorIdAsync(moderatorId);
        return Ok(gymGroups);
    }

    // Moderator can create groups for their gym, Admin can create any
    [HttpPost]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<ActionResult<GymGroupDTO>> CreateGymGroup([FromBody] CreateGymGroupDTO createGymGroupDto)
    {
        var gymGroup = await _gymGroupManager.CreateGymGroupAsync(createGymGroupDto);
        return CreatedAtAction(nameof(GetGymGroupById), new { id = gymGroup.Id }, gymGroup);
    }

    // Moderator can update their own groups, Admin can update any
    [HttpPut("{id}")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<ActionResult<GymGroupDTO>> UpdateGymGroup(int id, [FromBody] UpdateGymGroupDTO updateGymGroupDto)
    {
        var gymGroup = await _gymGroupManager.UpdateGymGroupAsync(id, updateGymGroupDto);
        if (gymGroup == null) return NotFound();
        return Ok(gymGroup);
    }

    // Moderator can delete their own groups, Admin can delete any
    [HttpDelete("{id}")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<ActionResult> DeleteGymGroup(int id)
    {
        var result = await _gymGroupManager.DeleteGymGroupAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}

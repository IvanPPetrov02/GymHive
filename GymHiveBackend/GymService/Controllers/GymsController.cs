using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using GymService.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GymsController : ControllerBase
{
    private readonly IGymManager _gymManager;
    private readonly IUserContextService _userContext;

    public GymsController(IGymManager gymManager, IUserContextService userContext)
    {
        _gymManager = gymManager;
        _userContext = userContext;
    }

    // Public - Anyone can view gyms
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GymDTO>>> GetAllGyms()
    {
        var gyms = await _gymManager.GetAllGymsAsync();
        return Ok(gyms);
    }

    // Public - Anyone can view a specific gym
    [HttpGet("{id}")]
    public async Task<ActionResult<GymDTO>> GetGymById(int id)
    {
        var gym = await _gymManager.GetGymByIdAsync(id);
        if (gym == null) return NotFound();
        return Ok(gym);
    }

    // Admin only - Create new gym
    [HttpPost]
    public async Task<ActionResult<GymDTO>> CreateGym([FromBody] CreateGymDTO createGymDto)
    {
        // Check if user is Admin
        if (!_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden: Admin role required" });
        }

        var gym = await _gymManager.CreateGymAsync(createGymDto);
        return CreatedAtAction(nameof(GetGymById), new { id = gym.Id }, gym);
    }

    // Admin only - Update gym
    [HttpPut("{id}")]
    public async Task<ActionResult<GymDTO>> UpdateGym(int id, [FromBody] UpdateGymDTO updateGymDto)
    {
        // Check if user is Admin
        if (!_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden: Admin role required" });
        }

        var gym = await _gymManager.UpdateGymAsync(id, updateGymDto);
        if (gym == null) return NotFound();
        return Ok(gym);
    }

    // Admin only - Delete gym
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGym(int id)
    {
        // Check if user is Admin
        if (!_userContext.IsInRole("Admin"))
        {
            return StatusCode(403, new { error = "Forbidden: Admin role required" });
        }

        var result = await _gymManager.DeleteGymAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}

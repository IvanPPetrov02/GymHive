using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GymsController : ControllerBase
{
    private readonly IGymManager _gymManager;

    public GymsController(IGymManager gymManager)
    {
        _gymManager = gymManager;
    }

    // Public - Anyone can view gyms
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GymDTO>>> GetAllGyms()
    {
        var gyms = await _gymManager.GetAllGymsAsync();
        return Ok(gyms);
    }

    // Public - Anyone can view a specific gym
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<GymDTO>> GetGymById(int id)
    {
        var gym = await _gymManager.GetGymByIdAsync(id);
        if (gym == null) return NotFound();
        return Ok(gym);
    }

    // Admin only - Create new gym
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GymDTO>> CreateGym([FromBody] CreateGymDTO createGymDto)
    {
        var gym = await _gymManager.CreateGymAsync(createGymDto);
        return CreatedAtAction(nameof(GetGymById), new { id = gym.Id }, gym);
    }

    // Admin only - Update gym
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GymDTO>> UpdateGym(int id, [FromBody] UpdateGymDTO updateGymDto)
    {
        var gym = await _gymManager.UpdateGymAsync(id, updateGymDto);
        if (gym == null) return NotFound();
        return Ok(gym);
    }

    // Admin only - Delete gym
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteGym(int id)
    {
        var result = await _gymManager.DeleteGymAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}

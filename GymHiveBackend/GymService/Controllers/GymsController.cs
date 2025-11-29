using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using GymService.Services;
using Microsoft.AspNetCore.Mvc;
using GymHive.Messaging.Interfaces;
using GymHive.Messaging.Events;

namespace GymService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GymsController : ControllerBase
{
    private readonly IGymManager _gymManager;
    private readonly IUserContextService _userContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<GymsController> _logger;

    public GymsController(IGymManager gymManager, IUserContextService userContext, IEventPublisher eventPublisher, ILogger<GymsController> logger)
    {
        _gymManager = gymManager;
        _userContext = userContext;
        _eventPublisher = eventPublisher;
        _logger = logger;
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
        
        // Publish GymCreatedEvent to RabbitMQ
        try
        {
            await _eventPublisher.PublishAsync(new GymCreatedEvent
            {
                GymId = gym.Id,
                Name = gym.Name,
                Location = gym.Address,
                CreatedBy = 0 // TODO: Fix user ID type mismatch (Guid vs int)
            });
            _logger.LogInformation($"Published GymCreatedEvent for gym {gym.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish GymCreatedEvent");
        }
        
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

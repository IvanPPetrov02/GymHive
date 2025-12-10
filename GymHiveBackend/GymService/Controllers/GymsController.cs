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

        // Create moderator users if specified
        if (createGymDto.Moderators != null && createGymDto.Moderators.Any())
        {
            try
            {
                var moderatorInfoList = createGymDto.Moderators.Select(m => new ModeratorInfo
                {
                    FirstName = m.FirstName,
                    LastName = m.LastName
                }).ToList();

                await _eventPublisher.PublishAsync(new CreateModeratorsCommand
                {
                    GymId = gym.Id,
                    GymName = gym.Name,
                    Moderators = moderatorInfoList
                });
                _logger.LogInformation($"Published CreateModeratorsCommand for gym {gym.Name} with {moderatorInfoList.Count} moderator(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish CreateModeratorsCommand");
            }
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

        // Create moderator users if specified
        if (updateGymDto.Moderators != null && updateGymDto.Moderators.Any())
        {
            try
            {
                var moderatorInfoList = updateGymDto.Moderators.Select(m => new ModeratorInfo
                {
                    FirstName = m.FirstName,
                    LastName = m.LastName
                }).ToList();

                await _eventPublisher.PublishAsync(new CreateModeratorsCommand
                {
                    GymId = id,
                    GymName = gym.Name,
                    Moderators = moderatorInfoList
                });
                _logger.LogInformation($"Published CreateModeratorsCommand for gym {gym.Name} with {moderatorInfoList.Count} moderator(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish CreateModeratorsCommand");
            }
        }

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

        // Get gym details before deletion
        var gym = await _gymManager.GetGymByIdAsync(id);
        if (gym == null) return NotFound();

        var result = await _gymManager.DeleteGymAsync(id);
        if (!result) return NotFound();

        // Publish GymDeletedEvent to trigger SAGA (delete memberships, groups, demote moderators)
        try
        {
            await _eventPublisher.PublishAsync(new GymDeletedEvent
            {
                GymId = id,
                GymName = gym.Name,
                DeletedBy = 0, // TODO: Get actual user ID
                DeletedAt = DateTime.UtcNow
            });
            _logger.LogInformation("✅ Published GymDeletedEvent for gym {GymId} ({GymName})", id, gym.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish GymDeletedEvent for gym {GymId}", id);
        }

        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkoutLoggingService.BLL.DTOs;
using WorkoutLoggingService.BLL.ManagerInterfaces;
using WorkoutLoggingService.Services;

namespace WorkoutLoggingService.Controllers;

[ApiController]
[Route("api/workouts")]
[Authorize]
public class WorkoutLogsController : ControllerBase
{
    private readonly IWorkoutLogManager _manager;
    private readonly IUserContextService _userContext;
    private readonly ILogger<WorkoutLogsController> _logger;

    public WorkoutLogsController(
        IWorkoutLogManager manager,
        IUserContextService userContext,
        ILogger<WorkoutLogsController> logger)
    {
        _manager = manager;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("my-logs")]
    public async Task<IActionResult> GetMyWorkoutLogs([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        try
        {
            var userId = _userContext.GetUserId();
            var logs = await _manager.GetUserWorkoutLogsAsync(userId, skip, take);
            return Ok(logs);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workout logs");
            return StatusCode(500, new { message = "An error occurred while retrieving workout logs" });
        }
    }

    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        try
        {
            var userId = _userContext.GetUserId();
            var workoutLog = await _manager.CheckInAsync(userId, request.GymId);
            return Ok(workoutLog);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in");
            return StatusCode(500, new { message = "An error occurred during check-in" });
        }
    }

    [HttpPut("{id}/checkout")]
    public async Task<IActionResult> CheckOut(int id)
    {
        try
        {
            var userId = _userContext.GetUserId();
            var workoutLog = await _manager.CheckOutAsync(userId, id);
            return Ok(workoutLog);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-out");
            return StatusCode(500, new { message = "An error occurred during check-out" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetWorkoutStats()
    {
        try
        {
            var userId = _userContext.GetUserId();
            var stats = await _manager.GetWorkoutStatsAsync(userId);
            return Ok(stats);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workout stats");
            return StatusCode(500, new { message = "An error occurred while retrieving workout stats" });
        }
    }
}

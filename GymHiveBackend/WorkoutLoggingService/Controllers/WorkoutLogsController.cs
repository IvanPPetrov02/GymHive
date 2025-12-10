using Microsoft.AspNetCore.Mvc;
using WorkoutLoggingService.BLL.DTOs;
using WorkoutLoggingService.BLL.ManagerInterfaces;
using WorkoutLoggingService.Services;

namespace WorkoutLoggingService.Controllers;

[ApiController]
[Route("api/workouts")]
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

    [HttpGet("my-workouts")]
    public async Task<IActionResult> GetMyWorkouts([FromQuery] string? startDate, [FromQuery] string? endDate)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            
            // Default to current week if no dates provided
            var start = string.IsNullOrEmpty(startDate) 
                ? DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek)
                : DateTime.Parse(startDate).Date;
            
            var end = string.IsNullOrEmpty(endDate)
                ? start.AddDays(6)
                : DateTime.Parse(endDate).Date;
            
            // Ensure we don't go into the future
            var today = DateTime.UtcNow.Date;
            if (start > today)
                start = today;
            if (end > today)
                end = today;
            
            // Ensure we don't go back more than 1 month
            var oneMonthAgo = today.AddMonths(-1);
            if (start < oneMonthAgo)
                start = oneMonthAgo;
            
            var visits = await _manager.GetGymVisitsAsync(userId, start, end);
            return Ok(visits);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gym visits");
            return StatusCode(500, new { message = "An error occurred while retrieving gym visits" });
        }
    }

    [HttpPost("log-visit")]
    public async Task<IActionResult> LogVisit([FromBody] LogGymVisitDTO dto)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            var visit = await _manager.LogGymVisitAsync(userId, dto);
            return Ok(visit);
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
            _logger.LogError(ex, "Error logging gym visit");
            return StatusCode(500, new { message = "An error occurred while logging gym visit" });
        }
    }

    [HttpDelete("{visitId}")]
    public async Task<IActionResult> DeleteVisit(int visitId)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            await _manager.DeleteGymVisitAsync(userId, visitId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gym visit");
            return StatusCode(500, new { message = "An error occurred while deleting gym visit" });
        }
    }
}


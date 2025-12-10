using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WorkoutLoggingService.BLL.DTOs;
using WorkoutLoggingService.BLL.ManagerInterfaces;
using WorkoutLoggingService.Controllers;
using WorkoutLoggingService.Services;
using Xunit;

namespace WorkoutLoggingService.Tests;

public class WorkoutLogsControllerTests
{
    private readonly Mock<IWorkoutLogManager> _mockManager;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly Mock<ILogger<WorkoutLogsController>> _mockLogger;
    private readonly WorkoutLogsController _controller;

    public WorkoutLogsControllerTests()
    {
        _mockManager = new Mock<IWorkoutLogManager>();
        _mockUserContext = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<WorkoutLogsController>>();
        _controller = new WorkoutLogsController(
            _mockManager.Object,
            _mockUserContext.Object,
            _mockLogger.Object);
    }

    #region GetMyWorkouts Tests

    [Fact]
    public async Task GetMyWorkouts_WithValidDates_ReturnsOkWithVisits()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var visits = new List<GymVisitDTO>
        {
            new GymVisitDTO
            {
                Id = 1,
                GymId = 1,
                VisitDate = DateTime.UtcNow.AddDays(-2)
            },
            new GymVisitDTO
            {
                Id = 2,
                GymId = 1,
                VisitDate = DateTime.UtcNow.AddDays(-1)
            }
        };

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockManager.Setup(m => m.GetGymVisitsAsync(
            userId,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()))
            .ReturnsAsync(visits);

        // Act
        var result = await _controller.GetMyWorkouts(startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedVisits = okResult.Value as IEnumerable<GymVisitDTO>;
        returnedVisits.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMyWorkouts_WithoutDates_UsesDefaultCurrentWeek()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var visits = new List<GymVisitDTO>();

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockManager.Setup(m => m.GetGymVisitsAsync(
            userId,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()))
            .ReturnsAsync(visits);

        // Act
        var result = await _controller.GetMyWorkouts(null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockManager.Verify(m => m.GetGymVisitsAsync(
            userId,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()), Times.Once);
    }

    #endregion

    #region LogVisit Tests

    [Fact]
    public async Task LogVisit_WithValidData_ReturnsCreatedVisit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logDto = new LogGymVisitDTO
        {
            GymId = 1
        };
        var createdVisit = new GymVisitDTO
        {
            Id = 1,
            GymId = logDto.GymId,
            VisitDate = DateTime.UtcNow
        };

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockManager.Setup(m => m.LogGymVisitAsync(userId, logDto))
            .ReturnsAsync(createdVisit);

        // Act
        var result = await _controller.LogVisit(logDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedVisit = okResult.Value as GymVisitDTO;
        returnedVisit.Should().NotBeNull();
        returnedVisit!.Id.Should().Be(1);
    }

    [Fact]
    public async Task LogVisit_WhenInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logDto = new LogGymVisitDTO { GymId = 1 };

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockManager.Setup(m => m.LogGymVisitAsync(userId, logDto))
            .ThrowsAsync(new InvalidOperationException("Invalid operation"));

        // Act
        var result = await _controller.LogVisit(logDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
    }

    #endregion

    #region DeleteVisit Tests

    [Fact]
    public async Task DeleteVisit_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var visitId = 1;

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockManager.Setup(m => m.DeleteGymVisitAsync(userId, visitId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteVisit(visitId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockManager.Verify(m => m.DeleteGymVisitAsync(userId, visitId), Times.Once);
    }

    [Fact]
    public async Task DeleteVisit_WhenVisitNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var visitId = 999;

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockManager.Setup(m => m.DeleteGymVisitAsync(userId, visitId))
            .ThrowsAsync(new KeyNotFoundException("Visit not found"));

        // Act
        var result = await _controller.DeleteVisit(visitId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}

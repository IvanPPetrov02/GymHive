using FluentAssertions;
using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using GymService.Controllers;
using GymService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using GymHive.Messaging.Interfaces;
using Microsoft.Extensions.Logging;

namespace GymService.Tests;

public class GymsControllerTests
{
    private readonly Mock<IGymManager> _mockGymManager;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<GymsController>> _mockLogger;
    private readonly GymsController _controller;

    public GymsControllerTests()
    {
        _mockGymManager = new Mock<IGymManager>();
        _mockUserContext = new Mock<IUserContextService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<GymsController>>();
        _controller = new GymsController(_mockGymManager.Object, _mockUserContext.Object, _mockEventPublisher.Object, _mockLogger.Object);
    }

    #region GetAllGyms Tests

    [Fact]
    public async Task GetAllGyms_ReturnsOkWithGymList()
    {
        // Arrange
        var gyms = new List<GymDTO>
        {
            new GymDTO { Id = 1, Name = "Gym One", Address = "Address 1" },
            new GymDTO { Id = 2, Name = "Gym Two", Address = "Address 2" }
        };
        
        _mockGymManager.Setup(m => m.GetAllGymsAsync())
            .ReturnsAsync(gyms);

        // Act
        var result = await _controller.GetAllGyms();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(gyms);
    }

    [Fact]
    public async Task GetAllGyms_WhenNoGyms_ReturnsEmptyList()
    {
        // Arrange
        _mockGymManager.Setup(m => m.GetAllGymsAsync())
            .ReturnsAsync(new List<GymDTO>());

        // Act
        var result = await _controller.GetAllGyms();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var gyms = okResult.Value as IEnumerable<GymDTO>;
        gyms.Should().BeEmpty();
    }

    #endregion

    #region GetGymById Tests

    [Fact]
    public async Task GetGymById_WithValidId_ReturnsOkWithGym()
    {
        // Arrange
        var gymId = 1;
        var gym = new GymDTO { Id = gymId, Name = "Test Gym", Address = "Test Address" };
        
        _mockGymManager.Setup(m => m.GetGymByIdAsync(gymId))
            .ReturnsAsync(gym);

        // Act
        var result = await _controller.GetGymById(gymId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(gym);
    }

    [Fact]
    public async Task GetGymById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var gymId = 999;
        _mockGymManager.Setup(m => m.GetGymByIdAsync(gymId))
            .ReturnsAsync((GymDTO)null);

        // Act
        var result = await _controller.GetGymById(gymId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region CreateGym Tests

    [Fact]
    public async Task CreateGym_AsAdmin_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateGymDTO { Name = "New Gym", Address = "New Address" };
        var createdGym = new GymDTO { Id = 1, Name = "New Gym", Address = "New Address" };
        
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockGymManager.Setup(m => m.CreateGymAsync(createDto))
            .ReturnsAsync(createdGym);

        // Act
        var result = await _controller.CreateGym(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.ActionName.Should().Be(nameof(GymsController.GetGymById));
        createdResult.RouteValues["id"].Should().Be(1);
        createdResult.Value.Should().BeEquivalentTo(createdGym);
    }

    [Fact]
    public async Task CreateGym_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var createDto = new CreateGymDTO { Name = "New Gym", Address = "New Address" };
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.CreateGym(createDto);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region UpdateGym Tests

    [Fact]
    public async Task UpdateGym_AsAdmin_ReturnsOkWithUpdatedGym()
    {
        // Arrange
        var gymId = 1;
        var updateDto = new UpdateGymDTO { Name = "Updated Gym", Address = "Updated Address" };
        var updatedGym = new GymDTO { Id = gymId, Name = "Updated Gym", Address = "Updated Address" };
        
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockGymManager.Setup(m => m.UpdateGymAsync(gymId, updateDto))
            .ReturnsAsync(updatedGym);

        // Act
        var result = await _controller.UpdateGym(gymId, updateDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(updatedGym);
    }

    [Fact]
    public async Task UpdateGym_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var gymId = 1;
        var updateDto = new UpdateGymDTO { Name = "Updated Gym", Address = "Updated Address" };
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.UpdateGym(gymId, updateDto);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateGym_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var gymId = 999;
        var updateDto = new UpdateGymDTO { Name = "Updated Gym", Address = "Updated Address" };
        
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockGymManager.Setup(m => m.UpdateGymAsync(gymId, updateDto))
            .ReturnsAsync((GymDTO)null);

        // Act
        var result = await _controller.UpdateGym(gymId, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DeleteGym Tests

    [Fact]
    public async Task DeleteGym_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        var gymId = 1;
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockGymManager.Setup(m => m.DeleteGymAsync(gymId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteGym(gymId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGym_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var gymId = 1;
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.DeleteGym(gymId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteGym_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var gymId = 999;
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockGymManager.Setup(m => m.DeleteGymAsync(gymId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteGym(gymId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}

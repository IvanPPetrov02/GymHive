using FluentAssertions;
using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using GymService.Controllers;
using GymService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymService.Tests;

public class GymGroupsControllerTests
{
    private readonly Mock<IGymGroupManager> _mockGymGroupManager;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly Mock<ILogger<GymGroupsController>> _mockLogger;
    private readonly GymGroupsController _controller;

    public GymGroupsControllerTests()
    {
        _mockGymGroupManager = new Mock<IGymGroupManager>();
        _mockUserContext = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<GymGroupsController>>();
        _controller = new GymGroupsController(_mockGymGroupManager.Object, _mockUserContext.Object, _mockLogger.Object);
    }

    #region GetAllGymGroups Tests

    [Fact]
    public async Task GetAllGymGroups_ReturnsOkWithGymGroupList()
    {
        // Arrange
        var gymGroups = new List<GymGroupDTO>
        {
            new GymGroupDTO { Id = 1, Name = "Group One", GymId = 1 },
            new GymGroupDTO { Id = 2, Name = "Group Two", GymId = 1 }
        };
        
        _mockGymGroupManager.Setup(m => m.GetAllGymGroupsAsync())
            .ReturnsAsync(gymGroups);

        // Act
        var result = await _controller.GetAllGymGroups();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(gymGroups);
    }

    [Fact]
    public async Task GetAllGymGroups_WhenNoGroups_ReturnsEmptyList()
    {
        // Arrange
        _mockGymGroupManager.Setup(m => m.GetAllGymGroupsAsync())
            .ReturnsAsync(new List<GymGroupDTO>());

        // Act
        var result = await _controller.GetAllGymGroups();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var groups = okResult.Value as IEnumerable<GymGroupDTO>;
        groups.Should().BeEmpty();
    }

    #endregion

    #region GetGymGroupById Tests

    [Fact]
    public async Task GetGymGroupById_WithValidId_ReturnsOkWithGymGroup()
    {
        // Arrange
        var groupId = 1;
        var gymGroup = new GymGroupDTO { Id = groupId, Name = "Test Group", GymId = 1 };
        
        _mockGymGroupManager.Setup(m => m.GetGymGroupByIdAsync(groupId))
            .ReturnsAsync(gymGroup);

        // Act
        var result = await _controller.GetGymGroupById(groupId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(gymGroup);
    }

    [Fact]
    public async Task GetGymGroupById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var groupId = 999;
        _mockGymGroupManager.Setup(m => m.GetGymGroupByIdAsync(groupId))
            .ReturnsAsync((GymGroupDTO)null);

        // Act
        var result = await _controller.GetGymGroupById(groupId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetGymGroupsByGymId Tests

    [Fact]
    public async Task GetGymGroupsByGymId_ReturnsOkWithGymGroups()
    {
        // Arrange
        var gymId = 1;
        var gymGroups = new List<GymGroupDTO>
        {
            new GymGroupDTO { Id = 1, Name = "Group One", GymId = gymId },
            new GymGroupDTO { Id = 2, Name = "Group Two", GymId = gymId }
        };
        
        _mockGymGroupManager.Setup(m => m.GetGymGroupsByGymIdAsync(gymId))
            .ReturnsAsync(gymGroups);

        // Act
        var result = await _controller.GetGymGroupsByGymId(gymId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(gymGroups);
    }

    #endregion

    #region GetGymGroupsByModeratorId Tests

    [Fact]
    public async Task GetGymGroupsByModeratorId_AsModerator_ReturnsOkWithGroups()
    {
        // Arrange
        var moderatorId = Guid.NewGuid();
        var gymGroups = new List<GymGroupDTO>
        {
            new GymGroupDTO { Id = 1, Name = "Group One", ModeratorId = moderatorId }
        };
        
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(true);
        _mockGymGroupManager.Setup(m => m.GetGymGroupsByModeratorIdAsync(moderatorId))
            .ReturnsAsync(gymGroups);

        // Act
        var result = await _controller.GetGymGroupsByModeratorId(moderatorId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(gymGroups);
    }

    [Fact]
    public async Task GetGymGroupsByModeratorId_AsAdmin_ReturnsOkWithGroups()
    {
        // Arrange
        var moderatorId = Guid.NewGuid();
        var gymGroups = new List<GymGroupDTO>
        {
            new GymGroupDTO { Id = 1, Name = "Group One", ModeratorId = moderatorId }
        };
        
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(false);
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockGymGroupManager.Setup(m => m.GetGymGroupsByModeratorIdAsync(moderatorId))
            .ReturnsAsync(gymGroups);

        // Act
        var result = await _controller.GetGymGroupsByModeratorId(moderatorId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(gymGroups);
    }

    [Fact]
    public async Task GetGymGroupsByModeratorId_AsUser_ReturnsForbidden()
    {
        // Arrange
        var moderatorId = Guid.NewGuid();
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(false);
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.GetGymGroupsByModeratorId(moderatorId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region CreateGymGroup Tests

    [Fact]
    public async Task CreateGymGroup_AsModerator_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateGymGroupDTO { Name = "New Group", GymId = 1 };
        var createdGroup = new GymGroupDTO { Id = 1, Name = "New Group", GymId = 1 };
        
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(true);
        _mockGymGroupManager.Setup(m => m.CreateGymGroupAsync(createDto))
            .ReturnsAsync(createdGroup);

        // Act
        var result = await _controller.CreateGymGroup(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.ActionName.Should().Be(nameof(GymGroupsController.GetGymGroupById));
        createdResult.RouteValues["id"].Should().Be(1);
        createdResult.Value.Should().BeEquivalentTo(createdGroup);
    }

    [Fact]
    public async Task CreateGymGroup_AsAdmin_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateGymGroupDTO { Name = "New Group", GymId = 1 };
        var createdGroup = new GymGroupDTO { Id = 1, Name = "New Group", GymId = 1 };
        
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(false);
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockGymGroupManager.Setup(m => m.CreateGymGroupAsync(createDto))
            .ReturnsAsync(createdGroup);

        // Act
        var result = await _controller.CreateGymGroup(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateGymGroup_AsUser_ReturnsForbidden()
    {
        // Arrange
        var createDto = new CreateGymGroupDTO { Name = "New Group", GymId = 1 };
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(false);
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.CreateGymGroup(createDto);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region UpdateGymGroup Tests

    [Fact]
    public async Task UpdateGymGroup_AsModerator_ReturnsOkWithUpdatedGroup()
    {
        // Arrange
        var groupId = 1;
        var updateDto = new UpdateGymGroupDTO { Name = "Updated Group" };
        var updatedGroup = new GymGroupDTO { Id = groupId, Name = "Updated Group", GymId = 1 };
        
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(true);
        _mockGymGroupManager.Setup(m => m.UpdateGymGroupAsync(groupId, updateDto))
            .ReturnsAsync(updatedGroup);

        // Act
        var result = await _controller.UpdateGymGroup(groupId, updateDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(updatedGroup);
    }

    [Fact]
    public async Task UpdateGymGroup_AsUser_ReturnsForbidden()
    {
        // Arrange
        var groupId = 1;
        var updateDto = new UpdateGymGroupDTO { Name = "Updated Group" };
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(false);
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.UpdateGymGroup(groupId, updateDto);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateGymGroup_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var groupId = 999;
        var updateDto = new UpdateGymGroupDTO { Name = "Updated Group" };
        
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(true);
        _mockGymGroupManager.Setup(m => m.UpdateGymGroupAsync(groupId, updateDto))
            .ReturnsAsync((GymGroupDTO)null);

        // Act
        var result = await _controller.UpdateGymGroup(groupId, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DeleteGymGroup Tests

    [Fact]
    public async Task DeleteGymGroup_AsModerator_ReturnsNoContent()
    {
        // Arrange
        var groupId = 1;
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(true);
        _mockGymGroupManager.Setup(m => m.DeleteGymGroupAsync(groupId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteGymGroup(groupId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGymGroup_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        var groupId = 1;
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(false);
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockGymGroupManager.Setup(m => m.DeleteGymGroupAsync(groupId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteGymGroup(groupId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGymGroup_AsUser_ReturnsForbidden()
    {
        // Arrange
        var groupId = 1;
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(false);
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.DeleteGymGroup(groupId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteGymGroup_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var groupId = 999;
        _mockUserContext.Setup(u => u.IsInRole("Moderator")).Returns(true);
        _mockGymGroupManager.Setup(m => m.DeleteGymGroupAsync(groupId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteGymGroup(groupId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}

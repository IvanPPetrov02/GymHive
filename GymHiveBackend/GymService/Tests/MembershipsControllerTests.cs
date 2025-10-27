using FluentAssertions;
using GymService.BLL.DTOs;
using GymService.BLL.ManagerInterfaces;
using GymService.Controllers;
using GymService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GymService.Tests;

public class MembershipsControllerTests
{
    private readonly Mock<IMembershipManager> _mockMembershipManager;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly MembershipsController _controller;

    public MembershipsControllerTests()
    {
        _mockMembershipManager = new Mock<IMembershipManager>();
        _mockUserContext = new Mock<IUserContextService>();
        _controller = new MembershipsController(_mockMembershipManager.Object, _mockUserContext.Object);
    }

    #region GetAllMemberships Tests

    [Fact]
    public async Task GetAllMemberships_AsAdmin_ReturnsOkWithMembershipList()
    {
        // Arrange
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = 1, UserId = Guid.NewGuid(), GymId = 1 },
            new MembershipDTO { Id = 2, UserId = Guid.NewGuid(), GymId = 2 }
        };
        
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockMembershipManager.Setup(m => m.GetAllMembershipsAsync())
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetAllMemberships();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(memberships);
    }

    [Fact]
    public async Task GetAllMemberships_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.GetAllMemberships();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region GetMembershipById Tests

    [Fact]
    public async Task GetMembershipById_WithValidId_ReturnsOkWithMembership()
    {
        // Arrange
        var membershipId = 1;
        var membership = new MembershipDTO { Id = membershipId, UserId = Guid.NewGuid(), GymId = 1 };
        
        _mockMembershipManager.Setup(m => m.GetMembershipByIdAsync(membershipId))
            .ReturnsAsync(membership);

        // Act
        var result = await _controller.GetMembershipById(membershipId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(membership);
    }

    [Fact]
    public async Task GetMembershipById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var membershipId = 999;
        _mockMembershipManager.Setup(m => m.GetMembershipByIdAsync(membershipId))
            .ReturnsAsync((MembershipDTO)null);

        // Act
        var result = await _controller.GetMembershipById(membershipId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetMembershipsByUserId Tests

    [Fact]
    public async Task GetMembershipsByUserId_ReturnsOkWithUserMemberships()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = 1, UserId = userId, GymId = 1 },
            new MembershipDTO { Id = 2, UserId = userId, GymId = 2 }
        };
        
        _mockMembershipManager.Setup(m => m.GetMembershipsByUserIdAsync(userId))
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetMembershipsByUserId(userId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(memberships);
    }

    #endregion

    #region GetMyMemberships Tests

    [Fact]
    public async Task GetMyMemberships_ReturnsOkWithCurrentUserMemberships()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = 1, UserId = userId, GymId = 1 }
        };
        
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(userId);
        _mockMembershipManager.Setup(m => m.GetMembershipsByUserIdAsync(userId))
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetMyMemberships();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(memberships);
    }

    #endregion

    #region GetMembershipsByGymId Tests

    [Fact]
    public async Task GetMembershipsByGymId_AsAdmin_ReturnsOkWithGymMemberships()
    {
        // Arrange
        var gymId = 1;
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = 1, UserId = Guid.NewGuid(), GymId = gymId },
            new MembershipDTO { Id = 2, UserId = Guid.NewGuid(), GymId = gymId }
        };
        
        _mockUserContext.Setup(u => u.GetCurrentUserRole()).Returns("Admin");
        _mockMembershipManager.Setup(m => m.GetMembershipsByGymIdAsync(gymId))
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetMembershipsByGymId(gymId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(memberships);
    }

    [Fact]
    public async Task GetMembershipsByGymId_AsModerator_ReturnsOkWithGymMemberships()
    {
        // Arrange
        var gymId = 1;
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = 1, UserId = Guid.NewGuid(), GymId = gymId }
        };
        
        _mockUserContext.Setup(u => u.GetCurrentUserRole()).Returns("Moderator");
        _mockMembershipManager.Setup(m => m.GetMembershipsByGymIdAsync(gymId))
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetMembershipsByGymId(gymId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(memberships);
    }

    [Fact]
    public async Task GetMembershipsByGymId_AsUser_ReturnsForbidden()
    {
        // Arrange
        var gymId = 1;
        _mockUserContext.Setup(u => u.GetCurrentUserRole()).Returns("User");

        // Act
        var result = await _controller.GetMembershipsByGymId(gymId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region CreateMembership Tests

    [Fact]
    public async Task CreateMembership_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createDto = new CreateMembershipDTO { GymId = 1 };
        var createdMembership = new MembershipDTO { Id = 1, UserId = userId, GymId = 1 };
        
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(userId);
        _mockMembershipManager.Setup(m => m.CreateMembershipAsync(userId, createDto))
            .ReturnsAsync(createdMembership);

        // Act
        var result = await _controller.CreateMembership(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.ActionName.Should().Be(nameof(MembershipsController.GetMembershipById));
        createdResult.RouteValues["id"].Should().Be(1);
        createdResult.Value.Should().BeEquivalentTo(createdMembership);
    }

    #endregion

    #region UpdateMembership Tests

    [Fact]
    public async Task UpdateMembership_WithValidData_ReturnsOkWithUpdatedMembership()
    {
        // Arrange
        var membershipId = 1;
        var updateDto = new UpdateMembershipDTO { MembershipType = "Premium", IsActive = true };
        var updatedMembership = new MembershipDTO { Id = membershipId, UserId = Guid.NewGuid(), GymId = 1, MembershipType = "Premium", IsActive = true };
        
        _mockMembershipManager.Setup(m => m.UpdateMembershipAsync(membershipId, updateDto))
            .ReturnsAsync(updatedMembership);

        // Act
        var result = await _controller.UpdateMembership(membershipId, updateDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(updatedMembership);
    }

    [Fact]
    public async Task UpdateMembership_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var membershipId = 999;
        var updateDto = new UpdateMembershipDTO { MembershipType = "Premium" };
        
        _mockMembershipManager.Setup(m => m.UpdateMembershipAsync(membershipId, updateDto))
            .ReturnsAsync((MembershipDTO?)null);

        // Act
        var result = await _controller.UpdateMembership(membershipId, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DeleteMembership Tests

    [Fact]
    public async Task DeleteMembership_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        var membershipId = 1;
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockMembershipManager.Setup(m => m.DeleteMembershipAsync(membershipId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteMembership(membershipId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteMembership_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var membershipId = 1;
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.DeleteMembership(membershipId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteMembership_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var membershipId = 999;
        _mockUserContext.Setup(u => u.IsInRole("Admin")).Returns(true);
        _mockMembershipManager.Setup(m => m.DeleteMembershipAsync(membershipId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteMembership(membershipId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}

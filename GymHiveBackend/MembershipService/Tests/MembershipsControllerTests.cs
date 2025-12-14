using FluentAssertions;
using GymHive.Messaging.Interfaces;
using MembershipService.BLL.DTOs;
using MembershipService.BLL.ManagerInterfaces;
using MembershipService.Controllers;
using MembershipService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MembershipService.Tests;

public class MembershipsControllerTests
{
    private readonly Mock<IMembershipManager> _mockMembershipManager;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<MembershipsController>> _mockLogger;
    private readonly MembershipsController _controller;

    public MembershipsControllerTests()
    {
        _mockMembershipManager = new Mock<IMembershipManager>();
        _mockUserContext = new Mock<IUserContextService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<MembershipsController>>();
        _controller = new MembershipsController(
            _mockMembershipManager.Object,
            _mockUserContext.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    #region GetAllMemberships Tests

    [Fact]
    public async Task GetAllMemberships_AsAdmin_ReturnsOkWithMembershipList()
    {
        // Arrange
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = "1", UserId = Guid.NewGuid(), GymId = 1, GymName = "Gym 1" },
            new MembershipDTO { Id = "2", UserId = Guid.NewGuid(), GymId = 2, GymName = "Gym 2" }
        };

        _mockUserContext.Setup(uc => uc.IsInRole("Admin")).Returns(true);
        _mockMembershipManager.Setup(m => m.GetAllMembershipsAsync())
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetAllMemberships();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMemberships = okResult.Value as IEnumerable<MembershipDTO>;
        returnedMemberships.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllMemberships_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        _mockUserContext.Setup(uc => uc.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.GetAllMemberships();

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region GetMembershipById Tests

    [Fact]
    public async Task GetMembershipById_WithValidId_ReturnsOkWithMembership()
    {
        // Arrange
        var membershipId = "123";
        var membership = new MembershipDTO
        {
            Id = membershipId,
            UserId = Guid.NewGuid(),
            GymId = 1,
            GymName = "Test Gym"
        };

        _mockMembershipManager.Setup(m => m.GetMembershipByIdAsync(membershipId))
            .ReturnsAsync(membership);

        // Act
        var result = await _controller.GetMembershipById(membershipId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMembership = okResult.Value as MembershipDTO;
        returnedMembership.Should().NotBeNull();
        returnedMembership!.Id.Should().Be(membershipId);
    }

    [Fact]
    public async Task GetMembershipById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var membershipId = "invalid";
        _mockMembershipManager.Setup(m => m.GetMembershipByIdAsync(membershipId))
            .ReturnsAsync((MembershipDTO?)null);

        // Act
        var result = await _controller.GetMembershipById(membershipId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetMembershipsByUserId Tests

    [Fact]
    public async Task GetMembershipsByUserId_ReturnsOkWithMemberships()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = "1", UserId = userId, GymId = 1, GymName = "Gym 1" },
            new MembershipDTO { Id = "2", UserId = userId, GymId = 2, GymName = "Gym 2" }
        };

        _mockMembershipManager.Setup(m => m.GetMembershipsByUserIdAsync(userId))
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetMembershipsByUserId(userId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMemberships = okResult.Value as IEnumerable<MembershipDTO>;
        returnedMemberships.Should().HaveCount(2);
    }

    #endregion

    #region GetMyMemberships Tests

    [Fact]
    public async Task GetMyMemberships_ReturnsOkWithUserMemberships()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = "1", UserId = userId, GymId = 1, GymName = "My Gym" }
        };

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockMembershipManager.Setup(m => m.GetMembershipsByUserIdAsync(userId))
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetMyMemberships();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMemberships = okResult.Value as IEnumerable<MembershipDTO>;
        returnedMemberships.Should().HaveCount(1);
    }

    #endregion

    #region GetMembershipsByGymId Tests

    [Fact]
    public async Task GetMembershipsByGymId_AsAdmin_ReturnsOkWithMemberships()
    {
        // Arrange
        var gymId = 1;
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = "1", UserId = Guid.NewGuid(), GymId = gymId, GymName = "Test Gym" }
        };

        _mockUserContext.Setup(uc => uc.GetCurrentUserRole()).Returns("Admin");
        _mockMembershipManager.Setup(m => m.GetMembershipsByGymIdAsync(gymId))
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetMembershipsByGymId(gymId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMemberships = okResult.Value as IEnumerable<MembershipDTO>;
        returnedMemberships.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMembershipsByGymId_AsModerator_ReturnsOkWithMemberships()
    {
        // Arrange
        var gymId = 1;
        var memberships = new List<MembershipDTO>
        {
            new MembershipDTO { Id = "1", UserId = Guid.NewGuid(), GymId = gymId, GymName = "Test Gym" }
        };

        _mockUserContext.Setup(uc => uc.GetCurrentUserRole()).Returns("Moderator");
        _mockMembershipManager.Setup(m => m.GetMembershipsByGymIdAsync(gymId))
            .ReturnsAsync(memberships);

        // Act
        var result = await _controller.GetMembershipsByGymId(gymId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMemberships = okResult.Value as IEnumerable<MembershipDTO>;
        returnedMemberships.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMembershipsByGymId_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        var gymId = 1;
        _mockUserContext.Setup(uc => uc.GetCurrentUserRole()).Returns("User");

        // Act
        var result = await _controller.GetMembershipsByGymId(gymId);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region CreateMembership Tests

    [Fact]
    public async Task CreateMembership_WithValidData_ReturnsCreatedMembership()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createDto = new CreateMembershipDTO
        {
            GymId = 1,
            MembershipType = "Monthly",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Price = 50.00m
        };
        var createdMembership = new MembershipDTO
        {
            Id = "new-id",
            UserId = userId,
            GymId = createDto.GymId,
            GymName = "Test Gym",
            MembershipType = createDto.MembershipType,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate,
            IsActive = true,
            Price = createDto.Price
        };

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockMembershipManager.Setup(m => m.CreateMembershipAsync(userId, createDto))
            .ReturnsAsync(createdMembership);

        // Act
        var result = await _controller.CreateMembership(createDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMembership = okResult.Value as MembershipDTO;
        returnedMembership.Should().NotBeNull();
        returnedMembership!.Id.Should().Be("new-id");
    }

    #endregion

    #region UpdateMembership Tests

    [Fact]
    public async Task UpdateMembership_AsAdmin_ReturnsOk()
    {
        // Arrange
        var membershipId = "123";
        var updateDto = new UpdateMembershipDTO
        {
            EndDate = DateTime.UtcNow.AddMonths(2),
            IsActive = true
        };

        var updatedMembership = new MembershipDTO { Id = membershipId, EndDate = updateDto.EndDate ?? DateTime.UtcNow.AddMonths(2), IsActive = updateDto.IsActive ?? true };

        _mockUserContext.Setup(uc => uc.IsInRole("Admin")).Returns(true);
        _mockMembershipManager.Setup(m => m.UpdateMembershipAsync(membershipId, updateDto))
            .ReturnsAsync(updatedMembership);

        // Act
        var result = await _controller.UpdateMembership(membershipId, updateDto);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateMembership_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var membershipId = "123";
        var updateDto = new UpdateMembershipDTO();

        _mockUserContext.Setup(uc => uc.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.UpdateMembership(membershipId, updateDto);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region DeleteMembership Tests

    [Fact]
    public async Task DeleteMembership_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        var membershipId = "123";

        _mockUserContext.Setup(uc => uc.IsInRole("Admin")).Returns(true);
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
        var membershipId = "123";

        _mockUserContext.Setup(uc => uc.IsInRole("Admin")).Returns(false);

        // Act
        var result = await _controller.DeleteMembership(membershipId);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(403);
    }

    #endregion
}

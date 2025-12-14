using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationsService.BLL.DTOs;
using NotificationsService.BLL.ManagerInterfaces;
using NotificationsService.Controllers;
using NotificationsService.Services;
using Xunit;

namespace NotificationsService.Tests;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationManager> _mockNotificationManager;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly Mock<ILogger<NotificationsController>> _mockLogger;
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _mockNotificationManager = new Mock<INotificationManager>();
        _mockUserContext = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<NotificationsController>>();
        _controller = new NotificationsController(
            _mockNotificationManager.Object,
            _mockUserContext.Object,
            _mockLogger.Object);
    }

    #region GetNotifications Tests

    [Fact]
    public async Task GetNotifications_WithValidUser_ReturnsOkWithNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<NotificationDTO>
        {
            new NotificationDTO
            {
                Id = 1,
                Type = "Info",
                Title = "Test Title 1",
                Message = "Test notification 1",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationDTO
            {
                Id = 2,
                Type = "Info",
                Title = "Test Title 2",
                Message = "Test notification 2",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockNotificationManager.Setup(m => m.GetUserNotificationsAsync(userId, 0, 20))
            .ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetNotifications(0, 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNotifications = okResult.Value as IEnumerable<NotificationDTO>;
        returnedNotifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotifications_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserContext.Setup(uc => uc.GetCurrentUserId())
            .Throws(new UnauthorizedAccessException("User not authenticated"));

        // Act
        var result = await _controller.GetNotifications(0, 20);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
    }

    #endregion

    #region GetUnreadNotifications Tests

    [Fact]
    public async Task GetUnreadNotifications_WithValidUser_ReturnsOnlyUnread()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var unreadNotifications = new List<NotificationDTO>
        {
            new NotificationDTO
            {
                Id = 1,
                Type = "Info",
                Title = "Unread Title",
                Message = "Unread notification",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockNotificationManager.Setup(m => m.GetUnreadNotificationsAsync(userId, 0, 20))
            .ReturnsAsync(unreadNotifications);

        // Act
        var result = await _controller.GetUnreadNotifications(0, 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNotifications = okResult.Value as IEnumerable<NotificationDTO>;
        returnedNotifications.Should().HaveCount(1);
        returnedNotifications!.All(n => !n.IsRead).Should().BeTrue();
    }

    #endregion

    #region GetUnreadCount Tests

    [Fact]
    public async Task GetUnreadCount_WithValidUser_ReturnsCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var unreadCountDto = new UnreadCountDTO { Count = 5 };

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockNotificationManager.Setup(m => m.GetUnreadCountAsync(userId))
            .ReturnsAsync(unreadCountDto);

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCount = okResult.Value as UnreadCountDTO;
        returnedCount.Should().NotBeNull();
        returnedCount!.Count.Should().Be(5);
    }

    #endregion

    #region MarkAsRead Tests

    [Fact]
    public async Task MarkAsRead_WithValidNotificationId_ReturnsNoContent()
    {
        // Arrange
        var notificationId = 1;
        var userId = Guid.NewGuid();

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockNotificationManager.Setup(m => m.MarkAsReadAsync(notificationId, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.MarkAsRead(notificationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockNotificationManager.Verify(m => m.MarkAsReadAsync(notificationId, userId), Times.Once);
    }

    #endregion

    #region MarkAllAsRead Tests

    [Fact]
    public async Task MarkAllAsRead_WithValidUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var markedCount = 5;

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockNotificationManager.Setup(m => m.MarkAllAsReadAsync(userId))
            .ReturnsAsync(markedCount);

        // Act
        var result = await _controller.MarkAllAsRead();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockNotificationManager.Verify(m => m.MarkAllAsReadAsync(userId), Times.Once);
    }

    #endregion

    #region DeleteNotification Tests

    [Fact]
    public async Task DeleteNotification_WithValidNotificationId_ReturnsNoContent()
    {
        // Arrange
        var notificationId = 1;
        var userId = Guid.NewGuid();

        _mockUserContext.Setup(uc => uc.GetCurrentUserId()).Returns(userId);
        _mockNotificationManager.Setup(m => m.DeleteAsync(notificationId, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockNotificationManager.Verify(m => m.DeleteAsync(notificationId, userId), Times.Once);
    }

    #endregion


}

using AuthenticationService.Controllers;
using BLL.DTOs;
using BLL.Entities;
using BLL.ManagerInterfaces;
using BLL.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;
using GymHive.Messaging.Interfaces;

namespace AuthenticationService.Tests;

public class AuthenticationControllerTests
{
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly Mock<ITokenValidationService> _mockTokenValidationService;
    private readonly Mock<ILogger<AuthenticationController>> _mockLogger;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly AuthenticationController _controller;
    private readonly IConfiguration _configuration;

    public AuthenticationControllerTests()
    {
        _mockUserManager = new Mock<IUserManager>();
        _mockTokenValidationService = new Mock<ITokenValidationService>();
        _mockLogger = new Mock<ILogger<AuthenticationController>>();
        _mockEventPublisher = new Mock<IEventPublisher>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_EMAILS_TOKEN"] = "test-token"
            })
            .Build();
        
        _controller = new AuthenticationController(
            _mockUserManager.Object,
            _mockTokenValidationService.Object,
            _mockLogger.Object,
            _mockEventPublisher.Object,
            _configuration);
        
        // Setup HttpContext for cookie tests
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Register Tests
    
    [Fact]
    public async Task Register_WithValidUser_ReturnsOkResult()
    {
        // Arrange
        var userDto = new UserRegisterDTO
        {
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            Password = "Password123!"
        };
        
        _mockUserManager.Setup(m => m.RegisterUserAsync(userDto))
            .ReturnsAsync("User created");

        // Act
        var result = await _controller.Register(userDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(new { message = "User created" });
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var userDto = new UserRegisterDTO
        {
            Email = "existing@example.com",
            Name = "Test",
            Surname = "User",
            Password = "Password123!"
        };
        
        _mockUserManager.Setup(m => m.RegisterUserAsync(userDto))
            .ReturnsAsync("Email already exists");

        // Act
        var result = await _controller.Register(userDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = "Email already exists" });
    }

    [Fact]
    public async Task Register_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var userDto = new UserRegisterDTO
        {
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            Password = "Password123!"
        };
        
        _mockUserManager.Setup(m => m.RegisterUserAsync(userDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Register(userDto);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new UserLoginDTO
        {
            Email = "test@example.com",
            Password = "Password123!"
        };
        
        var token = "valid.jwt.token";
        _mockUserManager.Setup(m => m.AuthenticateUserAsync(loginDto.Email, loginDto.Password))
            .ReturnsAsync(token);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(new { token });
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new UserLoginDTO
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };
        
        _mockUserManager.Setup(m => m.AuthenticateUserAsync(loginDto.Email, loginDto.Password))
            .ReturnsAsync((string)null);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Value.Should().Be("Authentication failed");
    }

    #endregion

    #region GetUser Tests

    [Fact]
    public async Task GetUser_WithValidUuid_ReturnsOkWithUser()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var user = new User
        {
            UUID = Guid.Parse(uuid),
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            Role = Role.User
        };
        
        _mockUserManager.Setup(m => m.GetUserByIdAsync(uuid))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUser(uuid);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedUser = okResult!.Value as User;
        returnedUser.Should().NotBeNull();
        returnedUser!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetUser_WithInvalidUuid_ReturnsNotFound()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        _mockUserManager.Setup(m => m.GetUserByIdAsync(uuid))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetUser(uuid);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsOk()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var updateDto = new UserUpdateDTO
        {
            Name = "Updated",
            Surname = "User"
        };
        
        _mockUserManager.Setup(m => m.UpdateUserDetailsAsync(uuid, updateDto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateUser(uuid, updateDto);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateUser_WhenExceptionThrown_Returns500()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var updateDto = new UserUpdateDTO
        {
            Name = "Updated",
            Surname = "User"
        };
        
        _mockUserManager.Setup(m => m.UpdateUserDetailsAsync(uuid, updateDto))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var result = await _controller.UpdateUser(uuid, updateDto);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_WithValidUuid_ReturnsNoContent()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        _mockUserManager.Setup(m => m.DeleteUserAsync(uuid))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteUser(uuid);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_WithInvalidUuid_ReturnsNotFound()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        _mockUserManager.Setup(m => m.DeleteUserAsync(uuid))
            .ThrowsAsync(new InvalidOperationException("User not found"));

        // Act
        var result = await _controller.DeleteUser(uuid);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetLoggedUser Tests

    [Fact]
    public async Task GetLoggedUser_WithValidToken_ReturnsOkWithUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            UUID = Guid.Parse(userId),
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            Role = Role.User
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;
        
        _mockUserManager.Setup(m => m.GetLoggedUserAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetLoggedUser();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedUser = okResult!.Value as User;
        returnedUser.Should().NotBeNull();
        returnedUser!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetLoggedUser_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        // Act
        var result = await _controller.GetLoggedUser();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithUserList()
    {
        // Arrange
        var users = new List<User>
        {
            new User { UUID = Guid.NewGuid(), Email = "user1@example.com", Name = "User", Surname = "One", Role = Role.User },
            new User { UUID = Guid.NewGuid(), Email = "user2@example.com", Name = "User", Surname = "Two", Role = Role.Admin }
        };
        
        _mockUserManager.Setup(m => m.GetAllUsersAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedUsers = okResult!.Value as IEnumerable<User>;
        returnedUsers.Should().NotBeNull();
        returnedUsers!.Count().Should().Be(2);
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public async Task ActivateUser_WithValidUuid_ReturnsOk()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        _mockUserManager.Setup(m => m.ActivateOrDeactivateUserAsync(uuid, true))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ActivateUser(uuid);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task DeactivateUser_WithValidUuid_ReturnsOk()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        _mockUserManager.Setup(m => m.ActivateOrDeactivateUserAsync(uuid, false))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeactivateUser(uuid);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var passwordChangeDto = new UserPasswordChangeDTO
        {
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };
        
        _mockUserManager.Setup(m => m.ChangePasswordAsync(uuid, passwordChangeDto.NewPassword, passwordChangeDto.OldPassword))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(uuid, passwordChangeDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_WithInvalidOldPassword_ReturnsUnauthorized()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var passwordChangeDto = new UserPasswordChangeDTO
        {
            OldPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };
        
        _mockUserManager.Setup(m => m.ChangePasswordAsync(uuid, passwordChangeDto.NewPassword, passwordChangeDto.OldPassword))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid old password"));

        // Act
        var result = await _controller.ChangePassword(uuid, passwordChangeDto);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Logout Tests

    [Fact]
    public void Logout_ReturnsOk()
    {
        // Act
        var result = _controller.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("success");
    }

    #endregion
}

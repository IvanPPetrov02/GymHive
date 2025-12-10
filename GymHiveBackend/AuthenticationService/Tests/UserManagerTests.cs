using BLL;
using BLL.DTOs;
using BLL.Entities;
using BLL.RepositoryInterfaces;
using BLL.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthenticationService.Tests;

public class UserManagerTests
{
    private readonly Mock<IUserDAO> _mockUserDao;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly UserManager _userManager;

    public UserManagerTests()
    {
        _mockUserDao = new Mock<IUserDAO>();
        _mockJwtService = new Mock<IJwtService>();
        _userManager = new UserManager(_mockUserDao.Object, _mockJwtService.Object);
    }

    #region RegisterUserAsync Tests

    [Fact]
    public async Task RegisterUserAsync_WithNewEmail_ReturnsUserCreated()
    {
        // Arrange
        var userDto = new UserRegisterDTO
        {
            Email = "newuser@example.com",
            Name = "New",
            Surname = "User",
            Password = "Password123!"
        };

        _mockUserDao.Setup(dao => dao.GetUserByEmailAsync(userDto.Email))
            .ReturnsAsync((User?)null);
        _mockUserDao.Setup(dao => dao.CreateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userManager.RegisterUserAsync(userDto);

        // Assert
        result.Should().Be("User created");
        _mockUserDao.Verify(dao => dao.CreateUserAsync(It.Is<User>(u =>
            u.Email == userDto.Email &&
            u.Name == userDto.Name &&
            u.Surname == userDto.Surname
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_WithExistingEmail_ReturnsUserAlreadyExists()
    {
        // Arrange
        var userDto = new UserRegisterDTO
        {
            Email = "existing@example.com",
            Name = "Existing",
            Surname = "User",
            Password = "Password123!"
        };

        var existingUser = new User
        {
            Email = userDto.Email,
            Name = "Existing",
            Surname = "User"
        };
        _mockUserDao.Setup(dao => dao.GetUserByEmailAsync(userDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _userManager.RegisterUserAsync(userDto);

        // Assert
        result.Should().Be("User already exists");
        _mockUserDao.Verify(dao => dao.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region AuthenticateUserAsync Tests

    [Fact]
    public async Task AuthenticateUserAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";
        var user = new User
        {
            UUID = Guid.NewGuid(),
            Email = email,
            Password = BLL.Encryption.PassHash.HashPassword(password),
            Name = "Test",
            Surname = "User",
            Role = Role.User
        };
        var expectedToken = "valid.jwt.token";

        _mockUserDao.Setup(dao => dao.GetUserByEmailAsync(email))
            .ReturnsAsync(user);
        _mockJwtService.Setup(jwt => jwt.GenerateJwtToken(user))
            .Returns(expectedToken);

        // Act
        var result = await _userManager.AuthenticateUserAsync(email, password);

        // Assert
        result.Should().Be(expectedToken);
    }

    [Fact]
    public async Task AuthenticateUserAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Password123!";

        _mockUserDao.Setup(dao => dao.GetUserByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userManager.AuthenticateUserAsync(email, password);

        // Assert
        result.Should().BeNull();
        _mockJwtService.Verify(jwt => jwt.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateUserAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var email = "test@example.com";
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword";
        var user = new User
        {
            UUID = Guid.NewGuid(),
            Email = email,
            Password = BLL.Encryption.PassHash.HashPassword(correctPassword),
            Name = "Test",
            Surname = "User"
        };

        _mockUserDao.Setup(dao => dao.GetUserByEmailAsync(email))
            .ReturnsAsync(user);

        // Act
        var result = await _userManager.AuthenticateUserAsync(email, wrongPassword);

        // Assert
        result.Should().BeNull();
        _mockJwtService.Verify(jwt => jwt.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region UpdateUserDetailsAsync Tests

    [Fact]
    public async Task UpdateUserDetailsAsync_WithValidData_UpdatesUser()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var existingUser = new User
        {
            UUID = Guid.Parse(uuid),
            Email = "old@example.com",
            Name = "Old",
            Surname = "Name"
        };
        var updateDto = new UserUpdateDTO
        {
            Email = "new@example.com",
            Name = "New",
            Surname = "Name"
        };

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync(existingUser);
        _mockUserDao.Setup(dao => dao.UpdateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userManager.UpdateUserDetailsAsync(uuid, updateDto);

        // Assert
        _mockUserDao.Verify(dao => dao.UpdateUserAsync(It.Is<User>(u =>
            u.Email == updateDto.Email &&
            u.Name == updateDto.Name &&
            u.Surname == updateDto.Surname
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateUserDetailsAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var updateDto = new UserUpdateDTO { Name = "New" };

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userManager.UpdateUserDetailsAsync(uuid, updateDto)
        );
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidUuid_DeletesUser()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var user = new User
        {
            UUID = Guid.Parse(uuid),
            Email = "test@example.com",
            Name = "Test",
            Surname = "User"
        };

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync(user);
        _mockUserDao.Setup(dao => dao.DeleteUserAsync(uuid))
            .Returns(Task.CompletedTask);

        // Act
        await _userManager.DeleteUserAsync(uuid);

        // Assert
        _mockUserDao.Verify(dao => dao.DeleteUserAsync(uuid), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userManager.DeleteUserAsync(uuid)
        );
    }

    #endregion

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { UUID = Guid.NewGuid(), Email = "user1@example.com", Name = "User", Surname = "One" },
            new User { UUID = Guid.NewGuid(), Email = "user2@example.com", Name = "User", Surname = "Two" }
        };

        _mockUserDao.Setup(dao => dao.GetAllUsersAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _userManager.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(users);
    }

    #endregion

    #region ActivateOrDeactivateUserAsync Tests

    [Fact]
    public async Task ActivateOrDeactivateUserAsync_WithValidUuid_UpdatesUserStatus()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var user = new User
        {
            UUID = Guid.Parse(uuid),
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            IsActive = true
        };

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync(user);
        _mockUserDao.Setup(dao => dao.UpdateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userManager.ActivateOrDeactivateUserAsync(uuid, false);

        // Assert
        _mockUserDao.Verify(dao => dao.UpdateUserAsync(It.Is<User>(u => u.IsActive == false)), Times.Once);
    }

    [Fact]
    public async Task ActivateOrDeactivateUserAsync_WithNonExistentUser_DoesNothing()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync((User?)null);

        // Act
        await _userManager.ActivateOrDeactivateUserAsync(uuid, false);

        // Assert
        _mockUserDao.Verify(dao => dao.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectOldPassword_ChangesPassword()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";
        var user = new User
        {
            UUID = Guid.Parse(uuid),
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            Password = BLL.Encryption.PassHash.HashPassword(oldPassword)
        };

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync(user);
        _mockUserDao.Setup(dao => dao.UpdateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userManager.ChangePasswordAsync(uuid, newPassword, oldPassword);

        // Assert
        _mockUserDao.Verify(dao => dao.UpdateUserAsync(It.Is<User>(u => u.Password != null)), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithIncorrectOldPassword_ThrowsException()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var oldPassword = "OldPassword123!";
        var wrongOldPassword = "WrongPassword";
        var newPassword = "NewPassword123!";
        var user = new User
        {
            UUID = Guid.Parse(uuid),
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            Password = BLL.Encryption.PassHash.HashPassword(oldPassword)
        };

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _userManager.ChangePasswordAsync(uuid, newPassword, wrongOldPassword)
        );
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userManager.ChangePasswordAsync(uuid, "NewPassword", "OldPassword")
        );
    }

    #endregion

    #region UpdateUserRoleAsync Tests

    [Fact]
    public async Task UpdateUserRoleAsync_WithValidUuid_UpdatesRole()
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

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync(user);
        _mockUserDao.Setup(dao => dao.UpdateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userManager.UpdateUserRoleAsync(uuid, Role.Admin);

        // Assert
        _mockUserDao.Verify(dao => dao.UpdateUserAsync(It.Is<User>(u => u.Role == Role.Admin)), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userManager.UpdateUserRoleAsync(uuid, Role.Admin)
        );
    }

    #endregion

    #region UpdateUserGymIdAsync Tests

    [Fact]
    public async Task UpdateUserGymIdAsync_WithValidUuid_UpdatesGymId()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();
        var user = new User
        {
            UUID = Guid.Parse(uuid),
            Email = "test@example.com",
            Name = "Test",
            Surname = "User",
            GymId = null
        };

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync(user);
        _mockUserDao.Setup(dao => dao.UpdateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userManager.UpdateUserGymIdAsync(uuid, 42);

        // Assert
        _mockUserDao.Verify(dao => dao.UpdateUserAsync(It.Is<User>(u => u.GymId == 42)), Times.Once);
    }

    [Fact]
    public async Task UpdateUserGymIdAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var uuid = Guid.NewGuid().ToString();

        _mockUserDao.Setup(dao => dao.GetUserByIdAsync(uuid))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userManager.UpdateUserGymIdAsync(uuid, 42)
        );
    }

    #endregion
}

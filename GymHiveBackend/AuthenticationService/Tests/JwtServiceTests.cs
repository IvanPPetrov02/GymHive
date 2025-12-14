using BLL.Entities;
using BLL.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace AuthenticationService.Tests;

public class JwtServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly JwtService _jwtService;
    private readonly string _testSecretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHS256Algorithm";

    public JwtServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["AppSettings:Token"]).Returns(_testSecretKey);
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("GymHiveAuthService");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("GymHiveGymService");
        
        _jwtService = new JwtService(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateJwtToken_WithValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            UUID = Guid.NewGuid(),
            Email = "test@example.com",
            Role = Role.User,
            Name = "Test",
            Surname = "User"
        };

        // Act
        var token = _jwtService.GenerateJwtToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        // The claim type is shortened in the token - check for "nameid", "email", "role"
        jwtToken.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == user.UUID.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == user.Role.ToString());
    }

    [Fact]
    public void GenerateJwtToken_WithAdminRole_ReturnsTokenWithAdminRole()
    {
        // Arrange
        var adminUser = new User
        {
            UUID = Guid.NewGuid(),
            Email = "admin@example.com",
            Role = Role.Admin,
            Name = "Admin",
            Surname = "User"
        };

        // Act
        var token = _jwtService.GenerateJwtToken(adminUser);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public void GenerateJwtToken_WithModeratorRole_ReturnsTokenWithModeratorRole()
    {
        // Arrange
        var moderatorUser = new User
        {
            UUID = Guid.NewGuid(),
            Email = "moderator@example.com",
            Role = Role.Moderator,
            Name = "Moderator",
            Surname = "User"
        };

        // Act
        var token = _jwtService.GenerateJwtToken(moderatorUser);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Moderator");
    }

    [Fact]
    public void GenerateJwtToken_TokenExpiresInOneDay()
    {
        // Arrange
        var user = new User
        {
            UUID = Guid.NewGuid(),
            Email = "test@example.com",
            Role = Role.User,
            Name = "Test",
            Surname = "User"
        };

        // Act
        var token = _jwtService.GenerateJwtToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var expirationTime = jwtToken.ValidTo;
        var expectedExpiration = DateTime.UtcNow.AddDays(1);
        
        expirationTime.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Constructor_WithMissingSecretKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["AppSettings:Token"]).Returns((string)null);

        // Act & Assert
        Action act = () => new JwtService(mockConfig.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT secret key must be set.");
    }

    [Fact]
    public void GenerateJwtToken_ContainsCorrectIssuerAndAudience()
    {
        // Arrange
        var user = new User
        {
            UUID = Guid.NewGuid(),
            Email = "test@example.com",
            Role = Role.User,
            Name = "Test",
            Surname = "User"
        };

        // Act
        var token = _jwtService.GenerateJwtToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Issuer.Should().Be("GymHiveAuthService");
        jwtToken.Audiences.Should().Contain("GymHiveGymService");
    }
}

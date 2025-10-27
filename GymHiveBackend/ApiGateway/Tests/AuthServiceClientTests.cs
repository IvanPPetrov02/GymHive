using ApiGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ApiGateway.Tests;

public class AuthServiceClientTests
{
    private readonly Mock<ILogger<AuthServiceClient>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public AuthServiceClientTests()
    {
        _mockLogger = new Mock<ILogger<AuthServiceClient>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["AuthService:BaseUrl"]).Returns("http://localhost:5010");
        
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
    }

    [Fact]
    public async Task IntrospectTokenAsync_WithValidToken_ReturnsActiveResponse()
    {
        // Arrange
        var token = "valid.jwt.token";
        var expectedResponse = new TokenValidationResponseDTO
        {
            Active = true,
            UserId = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            Role = "User"
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await client.IntrospectTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeTrue();
        result.UserId.Should().Be(expectedResponse.UserId);
        result.Email.Should().Be(expectedResponse.Email);
        result.Role.Should().Be(expectedResponse.Role);
    }

    [Fact]
    public async Task IntrospectTokenAsync_WithInvalidToken_ReturnsInactiveResponse()
    {
        // Arrange
        var token = "invalid.jwt.token";
        var expectedResponse = new TokenValidationResponseDTO
        {
            Active = false,
            Error = "Invalid token"
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await client.IntrospectTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
        result.Error.Should().Be("Invalid token");
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenAuthServiceReturnsError_ReturnsInactiveWithError()
    {
        // Arrange
        var token = "test.jwt.token";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await client.IntrospectTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
        result.Error.Should().Contain("AuthService returned");
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenHttpRequestFails_ReturnsInactiveWithError()
    {
        // Arrange
        var token = "test.jwt.token";
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await client.IntrospectTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
        result.Error.Should().Be("AuthService unavailable");
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenDeserializationFails_ReturnsInactiveWithError()
    {
        // Arrange
        var token = "test.jwt.token";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await client.IntrospectTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
    }

    [Fact]
    public async Task IntrospectTokenAsync_WithAdminRole_ReturnsActiveWithAdminRole()
    {
        // Arrange
        var token = "admin.jwt.token";
        var expectedResponse = new TokenValidationResponseDTO
        {
            Active = true,
            UserId = Guid.NewGuid().ToString(),
            Email = "admin@example.com",
            Role = "Admin"
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await client.IntrospectTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeTrue();
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task IntrospectTokenAsync_WithModeratorRole_ReturnsActiveWithModeratorRole()
    {
        // Arrange
        var token = "moderator.jwt.token";
        var expectedResponse = new TokenValidationResponseDTO
        {
            Active = true,
            UserId = Guid.NewGuid().ToString(),
            Email = "moderator@example.com",
            Role = "Moderator"
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await client.IntrospectTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeTrue();
        result.Role.Should().Be("Moderator");
    }

    [Fact]
    public void Constructor_InitializesWithCorrectEndpoint()
    {
        // Arrange & Act
        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Assert
        client.Should().NotBeNull();
        _mockConfiguration.Verify(c => c["AuthService:BaseUrl"], Times.Once);
    }

    [Fact]
    public async Task IntrospectTokenAsync_SendsCorrectRequestFormat()
    {
        // Arrange
        var token = "test.token";
        HttpRequestMessage capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new TokenValidationResponseDTO { Active = false }))
            });

        var client = new AuthServiceClient(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        await client.IntrospectTokenAsync(token);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri.ToString().Should().Contain("/api/Authentication/introspect");
    }
}

// DTO classes for testing (these should match your actual DTOs)
public class TokenValidationRequestDTO
{
    public string Token { get; set; }
}

public class TokenValidationResponseDTO
{
    public bool Active { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string Error { get; set; }
}

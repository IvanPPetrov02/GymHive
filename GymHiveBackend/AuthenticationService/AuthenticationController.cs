using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BLL.DTOs;
using BLL.Entities;
using BLL.ManagerInterfaces;
using BLL.Services;
using Microsoft.Extensions.Logging;
using GymHive.Messaging.Interfaces;
using GymHive.Messaging.Events;

namespace AuthenticationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserManager _userManager;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly IConfiguration _configuration;

        public AuthenticationController(
            IUserManager userManager, 
            ITokenValidationService tokenValidationService,
            ILogger<AuthenticationController> logger,
            IEventPublisher eventPublisher,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenValidationService = tokenValidationService;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("admin-emails")]
        public async Task<IActionResult> GetAdminEmails()
        {
            var expectedToken = (_configuration["ADMIN_EMAILS_TOKEN"] ?? "").Trim();
            if (string.IsNullOrEmpty(expectedToken))
            {
                _logger.LogError("ADMIN_EMAILS_TOKEN is not configured");
                return StatusCode(500, new { message = "Admin emails endpoint not configured" });
            }

            var providedToken = (Request.Headers["X-GymHive-AdminEmails-Token"].ToString() ?? "").Trim();
            if (string.IsNullOrEmpty(providedToken) || !string.Equals(providedToken, expectedToken, StringComparison.Ordinal))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var users = await _userManager.GetAllUsersAsync();
            var emails = users
                .Where(u => u.Role == Role.Admin)
                .Select(u => u.Email)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Ok(new { emails });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO userDto)
        {
            _logger.LogInformation("Register endpoint hit");

            try
            {
                var result = await _userManager.RegisterUserAsync(userDto);
                if (result == "User created")
                {
                    _logger.LogInformation("User created successfully");
                    
                    // Publish UserRegisteredEvent to RabbitMQ
                    try
                    {
                        await _eventPublisher.PublishAsync(new UserRegisteredEvent
                        {
                            UserId = Guid.Empty, // TODO: Get actual user ID from manager after registration
                            Email = userDto.Email,
                            Username = userDto.Email,
                            RoleId = 1 // Default role
                        });
                        _logger.LogInformation("Published UserRegisteredEvent");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish UserRegisteredEvent");
                        // Don't fail the registration if event publishing fails
                    }
                    
                    return Ok(new { message = result });
                }
                else
                {
                    _logger.LogWarning("Registration failed: {Result}", result);
                    return BadRequest(new { message = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the registration");
                return StatusCode(500, "Internal server error");
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO loginDto)
        {
            var token = await _userManager.AuthenticateUserAsync(loginDto.Email, loginDto.Password);
            if (token != null)
            {
                Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.None
                });
                return Ok(new { token });
            }
            return Unauthorized("Authentication failed");
        }

        [Authorize]
        [HttpGet("{uuid}")]
        public async Task<IActionResult> GetUser(string uuid)
        {
            if (!CanAccessUserResource(uuid))
            {
                return Forbid();
            }

            var user = await _userManager.GetUserByIdAsync(uuid);
            if (user == null) return NotFound();
            
            var userDto = MapUserToDto(user);
            return Ok(userDto);
        }

        [Authorize]
        [HttpPut("{uuid}")]
        public async Task<IActionResult> UpdateUser(string uuid, [FromBody] UserUpdateDTO userDto)
        {
            try
            {
                if (!CanAccessUserResource(uuid))
                {
                    return Forbid();
                }

                await _userManager.UpdateUserDetailsAsync(uuid, userDto);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the user");
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("{uuid}")]
        public async Task<IActionResult> DeleteUser(string uuid)
        {
            try
            {
                if (!CanAccessUserResource(uuid))
                {
                    return Forbid();
                }

                // Get user info before deletion
                var user = await _userManager.GetUserByIdAsync(uuid);
                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                // Delete user from authentication database
                await _userManager.DeleteUserAsync(uuid);

                // Publish UserDeletedEvent - triggers choreographed SAGA
                // Other services will react to this event and clean up their data
                var sagaId = Guid.NewGuid().ToString("N");
                _logger.LogInformation("Publishing UserDeletedEvent. SagaId: {SagaId}", sagaId);
                await _eventPublisher.PublishAsync(new UserDeletedEvent
                {
                    UserId = Guid.Parse(uuid),
                    Email = user.Email,
                    SagaId = sagaId,
                    DeletedAt = DateTime.UtcNow
                });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User not found for deletion: " + ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the user");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetLoggedUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var user = await _userManager.GetLoggedUserAsync(userIdClaim.Value);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            var userDto = MapUserToDto(user);
            return Ok(userDto);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.GetAllUsersAsync();
            var userDtos = users.Select(MapUserToDto);
            return Ok(userDtos);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("activate/{uuid}")]
        public async Task<IActionResult> ActivateUser(string uuid)
        {
            await _userManager.ActivateOrDeactivateUserAsync(uuid, true);
            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("deactivate/{uuid}")]
        public async Task<IActionResult> DeactivateUser(string uuid)
        {
            await _userManager.ActivateOrDeactivateUserAsync(uuid, false);
            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("update-role/{uuid}")]
        public async Task<IActionResult> UpdateUserRole(string uuid, [FromBody] UpdateRoleDTO updateRoleDto)
        {
            try
            {
                if (!Enum.TryParse<Role>(updateRoleDto.Role, true, out var role))
                {
                    return BadRequest(new { Message = "Invalid role. Valid roles are: User, Moderator, Admin" });
                }

                await _userManager.UpdateUserRoleAsync(uuid, role);
                return Ok(new { Message = $"User role updated to {role}" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-password/{uuid}")]
        public async Task<IActionResult> ChangePassword(string uuid, [FromBody] UserPasswordChangeDTO passwordChangeDto)
        {
            try
            {
                if (!CanAccessUserResource(uuid))
                {
                    return Forbid();
                }

                await _userManager.ChangePasswordAsync(uuid, passwordChangeDto.NewPassword, passwordChangeDto.OldPassword);
                return Ok(new { Message = "Password successfully changed." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
            };

            Response.Cookies.Append("jwt", "", cookieOptions);

            return Ok("success");
        }

        /// <summary>
        /// OAuth 2.0 Token Introspection endpoint (RFC 7662)
        /// Used by other microservices to validate JWT tokens
        /// </summary>
        [HttpPost("introspect")]
        [AllowAnonymous] // Services don't authenticate themselves in this simple implementation
        public async Task<IActionResult> IntrospectToken([FromBody] TokenValidationRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { error = "Token is required" });
            }

            // Remove "Bearer " prefix if present
            var token = request.Token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? request.Token.Substring(7)
                : request.Token;

            var result = await _tokenValidationService.ValidateTokenAsync(token);

            // Log validation attempt for security auditing
            _logger.LogInformation(
                "Token introspection: Active={Active}, Error={Error}",
                result.Active,
                result.Error
            );

            return Ok(result);
        }

        /// <summary>
        /// Admin-only endpoint to create moderator users with auto-generated emails
        /// Email format: firstname.lastname@gymname.com (incrementing 02, 03, etc. if duplicate)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("create-moderator")]
        public async Task<IActionResult> CreateModerator([FromBody] CreateModeratorDTO moderatorDto)
        {
            try
            {
                _logger.LogInformation("CreateModerator called for gym {GymId}", moderatorDto.GymId);

                // Generate email: firstname.lastname@gymname.com
                var gymNameSlug = moderatorDto.GymName.ToLower().Replace(" ", "");
                var baseEmail = $"{moderatorDto.FirstName.ToLower()}.{moderatorDto.LastName.ToLower()}@{gymNameSlug}.com";
                var email = baseEmail;
                var counter = 1;

                // Check if email exists and increment with 02, 03, etc.
                var existingUser = await _userManager.GetUserByEmailAsync(email);
                while (existingUser != null)
                {
                    counter++;
                    email = $"{moderatorDto.FirstName.ToLower()}.{moderatorDto.LastName.ToLower()}{counter:D2}@{gymNameSlug}.com";
                    existingUser = await _userManager.GetUserByEmailAsync(email);
                }

                // Register the user with default password
                var defaultPassword = "Moderator123!";
                await _userManager.RegisterUserAsync(new BLL.DTOs.UserRegisterDTO
                {
                    Email = email,
                    Password = defaultPassword,
                    Name = moderatorDto.FirstName,
                    Surname = moderatorDto.LastName
                });

                // Immediately update role to Moderator and set GymId
                var createdUser = await _userManager.GetUserByEmailAsync(email);
                if (createdUser != null)
                {
                    _logger.LogInformation("Updating role and GymId for created moderator. GymId: {GymId}", moderatorDto.GymId);
                    
                    await _userManager.UpdateUserRoleAsync(createdUser.UUID.ToString(), Role.Moderator);
                    
                    await _userManager.UpdateUserGymIdAsync(createdUser.UUID.ToString(), moderatorDto.GymId);
                    
                    // Verify the GymId was actually set
                    var verifyUser = await _userManager.GetUserByEmailAsync(email);
                    _logger.LogInformation("Created moderator for gym {GymId}. GymId set: {GymIdSet}", moderatorDto.GymId, verifyUser?.GymId);

                    // Publish event so GymService can link this moderator to the gym
                    try
                    {
                        await _eventPublisher.PublishAsync(new ModeratorsCreatedEvent
                        {
                            GymId = moderatorDto.GymId,
                            Moderators = new List<CreatedModeratorInfo>
                            {
                                new CreatedModeratorInfo
                                {
                                    UserId = createdUser.UUID,
                                    Email = email,
                                    FirstName = moderatorDto.FirstName,
                                    LastName = moderatorDto.LastName
                                }
                            }
                        });
                        _logger.LogInformation("✅ Published ModeratorsCreatedEvent for gym {GymId}", moderatorDto.GymId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish ModeratorsCreatedEvent");
                    }

                    return Ok(new { 
                        message = "Moderator created successfully",
                        email = email,
                        userId = createdUser.UUID,
                        defaultPassword = defaultPassword
                    });
                }
                else
                {
                    _logger.LogError("Failed to retrieve created user");
                    return StatusCode(500, new { message = "Failed to create moderator" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR creating moderator");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        private bool CanAccessUserResource(string uuid)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return false;
            }

            if (!Guid.TryParse(userIdClaim, out var currentUserId))
            {
                return false;
            }

            if (!Guid.TryParse(uuid, out var targetUserId))
            {
                return false;
            }

            return currentUserId == targetUserId;
        }

        // Helper method to map User entity to UserDTO with role as string
        private UserDTO MapUserToDto(BLL.Entities.User user)
        {
            return new UserDTO
            {
                UUID = user.UUID.ToString(),
                Email = user.Email,
                Name = user.Name,
                Surname = user.Surname,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Role = user.Role.ToString(), // Converts enum to string: "User", "Moderator", or "Admin"
                GymId = user.GymId
            };
        }
    }
}

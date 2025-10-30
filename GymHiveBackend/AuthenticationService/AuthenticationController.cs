using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BLL.DTOs;
using BLL.ManagerInterfaces;
using BLL.Services;
using Microsoft.Extensions.Logging;

namespace AuthenticationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserManager _userManager;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            IUserManager userManager, 
            ITokenValidationService tokenValidationService,
            ILogger<AuthenticationController> logger)
        {
            _userManager = userManager;
            _tokenValidationService = tokenValidationService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO userDto)
        {
            _logger.LogInformation("Register endpoint hit");
            _logger.LogInformation($"Email: {userDto.Email}, Name: {userDto.Name}, Surname: {userDto.Surname}");

            try
            {
                var result = await _userManager.RegisterUserAsync(userDto);
                if (result == "User created")
                {
                    _logger.LogInformation("User created successfully");
                    return Ok(new { message = result });
                }
                else
                {
                    _logger.LogWarning($"Registration failed: {result}");
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
                await _userManager.DeleteUserAsync(uuid);
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

        [Authorize]
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

        [Authorize]
        [HttpPost("change-password/{uuid}")]
        public async Task<IActionResult> ChangePassword(string uuid, [FromBody] UserPasswordChangeDTO passwordChangeDto)
        {
            try
            {
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
                "Token introspection: Active={Active}, UserId={UserId}, Error={Error}",
                result.Active,
                result.UserId,
                result.Error
            );

            return Ok(result);
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
                Role = user.Role.ToString() // Converts enum to string: "User", "Moderator", or "Admin"
            };
        }
    }
}

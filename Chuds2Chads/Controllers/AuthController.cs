using Chuds2Chads.Data;
using Chuds2Chads.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Chuds2Chads.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly MultiplayerService _multiplayerService;
        private readonly AvatarService _avatarService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            MultiplayerService multiplayerService,
            AvatarService avatarService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _multiplayerService = multiplayerService;
            _avatarService = avatarService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
                return BadRequest(new { error = "Username already taken." });

            var email = request.Username + "@chuds2chads.local";
            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail != null)
                return BadRequest(new { error = "Email already in use." });

            IdentityResult? result = null;
            Guid? createdUserId = null;
            const int maxAttempts = 5;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = request.Username,
                    Email = email,
                    CreatedDate = DateTime.UtcNow,
                    LastSeenUtc = DateTime.UtcNow,
                    PresenceStatus = UserPresenceStatus.Offline
                };

                user.FriendCode = await _multiplayerService.GenerateUniqueFriendCodeAsync(user.Id);

                try
                {
                    result = await _userManager.CreateAsync(user, request.Password);
                    if (result.Succeeded)
                    {
                        createdUserId = user.Id;
                        await _multiplayerService.EnsureUserSetupAsync(user.Id);
                        await _avatarService.EnsureUserAvatarInitializedAsync(user.Id);
                        return Ok(new { message = "Account created successfully!" });
                    }

                    break;
                }
                catch (DbUpdateException ex) when (IsFriendCodeConflict(ex))
                {
                    if (attempt == maxAttempts - 1)
                    {
                        return BadRequest(new { error = "Could not generate a unique friend code. Please try again." });
                    }
                }
            }

            if (createdUserId.HasValue)
            {
                await _avatarService.EnsureUserAvatarInitializedAsync(createdUserId.Value);
            }

            var errors = result is null
                ? "Registration failed."
                : string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _userManager.FindByNameAsync(request.Username) ??
                       await _userManager.FindByEmailAsync(request.Username);

            if (user == null)
            {
                return Unauthorized(new { error = "Invalid username or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { error = "Invalid username or password." });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            await _multiplayerService.EnsureUserSetupAsync(user.Id);
            await _avatarService.EnsureUserAvatarInitializedAsync(user.Id);
            await _multiplayerService.SetPresenceAsync(user.Id, UserPresenceStatus.Online);

            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");

            return Ok(new
            {
                message = "Login successful!",
                userId = user.Id,
                userName = user.UserName,
                email = user.Email,
                isAdmin
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                await _multiplayerService.SetPresenceAsync(user.Id, UserPresenceStatus.Offline);
            }

            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            await _multiplayerService.EnsureUserSetupAsync(user.Id);
            await _avatarService.EnsureUserAvatarInitializedAsync(user.Id);

            return Ok(new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                createdDate = user.CreatedDate,
                friendCode = user.FriendCode
            });
        }

        private static bool IsFriendCodeConflict(DbUpdateException exception)
        {
            return exception.InnerException is SqliteException sqliteException &&
                   sqliteException.SqliteErrorCode == 19 &&
                   sqliteException.Message.Contains("AspNetUsers.FriendCode", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAppApi.Models;
using WebAppApi.Services;

namespace WebAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly HashSet<string> AllowedAvatarExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly PermissionService _permissionService;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        PermissionService permissionService,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _permissionService = permissionService;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new IdentityRole("User"));

        await _userManager.AddToRoleAsync(user, "User");

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password" });

        if (!user.IsActive)
            return Unauthorized(new { message = "Account is inactive" });

        var (token, expiration) = await _tokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);

        return Ok(ToAuthResponse(user, token, expiration, roles, allowedPages));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!await _roleManager.RoleExistsAsync(request.Role))
            await _roleManager.CreateAsync(new IdentityRole(request.Role));

        var result = await _userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = $"Role '{request.Role}' assigned to '{request.Email}'" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);
        return Ok(new
        {
            user.Email,
            user.UserName,
            user.FullName,
            user.AvatarUrl,
            user.IsActive,
            Roles = roles,
            AllowedPages = allowedPages
        });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user == null)
            return NotFound(new { message = "User not found" });

        var newEmail = request.Email.Trim();
        var newFullName = request.FullName.Trim();

        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userManager.FindByEmailAsync(newEmail);
            if (existing != null && existing.Id != user.Id)
                return BadRequest(new { message = "Email already in use" });

            var emailResult = await _userManager.SetEmailAsync(user, newEmail);
            if (!emailResult.Succeeded)
                return BadRequest(emailResult.Errors);

            var userNameResult = await _userManager.SetUserNameAsync(user, newEmail);
            if (!userNameResult.Succeeded)
                return BadRequest(userNameResult.Errors);
        }

        user.FullName = newFullName;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        var (token, expiration) = await _tokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);

        return Ok(ToAuthResponse(user, token, expiration, roles, allowedPages));
    }

    [Authorize]
    [HttpPost("avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedAvatarExtensions.Contains(extension))
            return BadRequest(new { message = "Only image files are allowed (.jpg, .jpeg, .png, .gif, .webp)" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "File size must be 5MB or less" });

        var user = await GetCurrentApplicationUserAsync();
        if (user == null)
            return NotFound(new { message = "User not found" });

        var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(uploadsFolder);

        var originalName = Path.GetFileNameWithoutExtension(file.FileName);
        // Sanitize filename
        foreach (var c in Path.GetInvalidFileNameChars())
            originalName = originalName.Replace(c, '_');
        if (string.IsNullOrWhiteSpace(originalName))
            originalName = "avatar";

        // originalname_{guid}.ext — GUID suffix to avoid duplicates
        var storedFileName = $"{originalName}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Delete old avatar file if it was under our uploads folder
        if (!string.IsNullOrWhiteSpace(user.AvatarUrl) &&
            user.AvatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(_env.ContentRootPath, "wwwroot",
                user.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldPath))
            {
                try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
            }
        }

        user.AvatarUrl = $"/uploads/avatars/{storedFileName}";
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        var (token, expiration) = await _tokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);

        return Ok(ToAuthResponse(user, token, expiration, roles, allowedPages));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user == null)
            return NotFound(new { message = "User not found" });

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Password changed successfully" });
    }

    private async Task<ApplicationUser?> GetCurrentApplicationUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        return await _userManager.FindByIdAsync(userId);
    }

    private static AuthResponse ToAuthResponse(
        ApplicationUser user,
        string token,
        DateTime expiration,
        IList<string> roles,
        IList<string>? allowedPages = null) => new()
    {
        Token = token,
        Expiration = expiration,
        Email = user.Email!,
        FullName = user.FullName,
        AvatarUrl = user.AvatarUrl,
        Roles = roles,
        AllowedPages = allowedPages ?? new List<string>()
    };
}

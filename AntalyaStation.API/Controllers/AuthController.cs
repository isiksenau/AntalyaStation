using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AntalyaStation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;

    public AuthController(IConfiguration configuration, IAuthService authService, IUserRepository userRepository)
    {
        _configuration = configuration;
        _authService = authService;
        _userRepository = userRepository;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        // Make sure a default admin account exists the first time the app runs.
        await _authService.EnsureDefaultAdminAsync();

        var user = await _authService.ValidateUserAsync(loginDto.Username, loginDto.Password);
        if (user == null)
            return Unauthorized(new { Message = "Invalid username or password." });

        var token = GenerateJwtToken(user.Username, user.Role, user.Id!);
        return Ok(new { Token = token, Message = "Login successful" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return NotFound(new { Message = "User not found." });

        return Ok(new UserProfileDto
        {
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            CreatedDate = user.CreatedDate
        });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return NotFound(new { Message = "User not found." });

        var updated = await _userRepository.UpdateProfileAsync(user.Id!, dto.FullName, dto.Email);
        if (!updated) return StatusCode(500, new { Message = "Profile could not be updated." });

        return Ok(new { Message = "Profile updated successfully." });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(new { Message = "New password and confirmation do not match." });

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest(new { Message = "New password must be at least 6 characters long." });

        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return NotFound(new { Message = "User not found." });

        if (!_authService.VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return BadRequest(new { Message = "Current password is incorrect." });

        var (hash, salt) = _authService.HashPassword(dto.NewPassword);
        var updated = await _userRepository.UpdatePasswordAsync(user.Id!, hash, salt);
        if (!updated) return StatusCode(500, new { Message = "Password could not be updated." });

        return Ok(new { Message = "Password changed successfully." });
    }

    private string GenerateJwtToken(string username, string role, string userId)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("uid", userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] LoginDto dto)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingUser != null)
        {
            return BadRequest(new { Message = "Bu kullanıcı adı zaten sistemde kayıtlı!" });
        }

        var (hash, salt) = _authService.HashPassword(dto.Password);

        
        var newUser = new User
        {
            Username = dto.Username,
            FullName = "System Administrator",
            Email = $"{dto.Username}@antalyastation.local",
            Role = "Admin", // 🟢 Yetkiyi Admin olarak sabitliyoruz
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedDate = DateTime.UtcNow
        };

        await _userRepository.AddAsync(newUser);

        return Ok(new { Message = $"'{dto.Username}' kullanıcı adı ve 'Admin' rolüyle başarıyla oluşturuldu!" });
    }
}
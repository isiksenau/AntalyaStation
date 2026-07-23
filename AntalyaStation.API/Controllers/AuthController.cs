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

        var token = GenerateJwtToken(user); // 🟢 artık tüm user nesnesini gönderiyoruz, çünkü permission claim'leri de oradan okunuyor
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
            Id = user.Id!,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
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

        if (string.IsNullOrWhiteSpace(dto.Username))
            return BadRequest(new { Message = "Username cannot be empty." });

        // 🟢 Kullanıcı adı değiştiriliyorsa, başka biri tarafından alınmış mı kontrol et
        if (!string.Equals(dto.Username, user.Username, StringComparison.OrdinalIgnoreCase))
        {
            var taken = await _userRepository.IsUsernameTakenAsync(dto.Username, user.Id);
            if (taken)
                return BadRequest(new { Message = "This username is already taken." });
        }

        var updated = await _userRepository.UpdateProfileAsync(user.Id!, dto.Username, dto.FullName, dto.Email, dto.PhoneNumber);
        if (!updated) return StatusCode(500, new { Message = "Profile could not be updated." });

        // 🟢 Kullanıcı adı değiştiyse yeni bir token üretmemiz lazım — eski token artık geçersiz kullanıcı adını taşıyor
// 🟢 Kullanıcı adı değişmiş olabilir, güncel bilgiyle yeni bir User nesnesi oluşturup token'ı ona göre üretiyoruz
        user.Username = dto.Username;
        var newToken = GenerateJwtToken(user);
        
        return Ok(new { Message = "Profile updated successfully.", Token = newToken });
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

    // AuthController.cs — GenerateJwtToken metodunu User nesnesi alacak şekilde değiştir
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("uid", user.Id!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 🟢 SuperAdmin her izne otomatik sahip — DB'de ayrıca tutmaya gerek yok
        if (user.Role == "SuperAdmin")
        {
            foreach (var p in PermissionCatalog.All)
                claims.Add(new Claim("permission", p.Key));
            claims.Add(new Claim("permission", "ManagePermissions")); // sadece SuperAdmin'e özel
        }
        else
        {
            foreach (var p in user.Permissions ?? new List<string>())
                claims.Add(new Claim("permission", p));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"]!)),
            signingCredentials: creds);

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
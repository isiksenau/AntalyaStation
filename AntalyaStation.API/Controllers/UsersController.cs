using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntalyaStation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;

    public UsersController(IUserRepository userRepository, IAuthService authService)
    {
        _userRepository = userRepository;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userRepository.GetAllAsync();
        var result = users.Select(u => new AdminUserListItemDto
        {
            Id = u.Id!,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role,
            CreatedDate = u.CreatedDate
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { Message = "Username and password are required." });

        if (dto.Password.Length < 6)
            return BadRequest(new { Message = "Password must be at least 6 characters long." });

        var taken = await _userRepository.IsUsernameTakenAsync(dto.Username);
        if (taken)
            return BadRequest(new { Message = "This username is already taken." });

        var (hash, salt) = _authService.HashPassword(dto.Password);

        var newUser = new User
        {
            Username = dto.Username,
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedDate = DateTime.UtcNow
        };

        await _userRepository.AddAsync(newUser);
        return Ok(new { Message = "User created successfully." });
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleDto dto)
    {
        if (dto.Role != "Admin" && dto.Role != "User")
            return BadRequest(new { Message = "Role must be either 'Admin' or 'User'." });

        var updated = await _userRepository.UpdateRoleAsync(id, dto.Role);
        if (!updated) return NotFound(new { Message = "User not found." });

        return Ok(new { Message = "Role updated successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var currentUsername = User.Identity?.Name;
        var currentUser = await _userRepository.GetByUsernameAsync(currentUsername ?? "");

        if (currentUser?.Id == id)
            return BadRequest(new { Message = "You cannot delete your own account while logged in." });

        var deleted = await _userRepository.DeleteAsync(id);
        if (!deleted) return NotFound(new { Message = "User not found." });

        return Ok(new { Message = "User deleted successfully." });
    }
}
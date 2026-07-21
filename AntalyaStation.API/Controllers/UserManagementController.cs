using AntalyaStation.API.DTOs;
using AntalyaStation.API.Repositories;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntalyaStation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UserManagementController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly IStationRepository _stationRepository;
    private readonly IExcelImportService _excelImportService;

    public UserManagementController(
        IUserRepository userRepository,
        IAuthService authService,
        IStationRepository stationRepository,
        IExcelImportService excelImportService)
    {
        _userRepository = userRepository;
        _authService = authService;
        _stationRepository = stationRepository;
        _excelImportService = excelImportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userRepository.GetAllAsync();
        var result = users.Select(u => new UserListItemDto
        {
            Id = u.Id!,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            CreatedDate = u.CreatedDate
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!CanAssignRole(dto.Role))
            return Forbid();

        var (success, error, user) = await _authService.CreateUserAsync(
            dto.Username, dto.Password, dto.FullName, dto.Email, dto.Role);

        if (!success)
            return BadRequest(new { Message = error });

        return Ok(new { Message = "User created successfully.", UserId = user!.Id });
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleDto dto)
    {
        if (!CanAssignRole(dto.Role))
            return Forbid();

        var updated = await _userRepository.UpdateRoleAsync(id, dto.Role);
        if (!updated) return NotFound(new { Message = "User not found." });

        return Ok(new { Message = "Role updated successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var deleted = await _userRepository.DeleteAsync(id);
        if (!deleted) return NotFound(new { Message = "User not found." });

        return Ok(new { Message = "User deleted successfully." });
    }

    private bool CanAssignRole(string? role)
    {
        if (role == "User") return true;
        if (role == "Admin") return User.IsInRole("SuperAdmin");
        return false;
    }

    [HttpGet("system-stats")]
    public async Task<IActionResult> GetSystemStats()
    {
        var stations = (await _stationRepository.GetAllAsync()).Count();
        var totalUsers = await _userRepository.CountAllAsync();
        var adminCount = await _userRepository.CountByRoleAsync("Admin");
        var batches = await _excelImportService.GetActiveImportBatchesAsync();

        var stats = new SystemStatsDto
        {
            TotalStations = stations,
            TotalUsers = (int)totalUsers,
            AdminCount = (int)adminCount,
            ActiveImportBatches = batches.Count,
            DatabaseName = "AntalyaStationDB",
            ApiVersion = "1.0.0"
        };

        return Ok(stats);
    }
}
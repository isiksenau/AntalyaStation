using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntalyaStation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")] // 🔴 SADECE SuperAdmin çağırabilir!
public class PermissionsController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public PermissionsController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("catalog")]
    public IActionResult GetCatalog()
    {
        var response = new PermissionCatalogResponseDto
        {
            AllPermissions = PermissionCatalog.All.Select(p => new PermissionDefinitionDto
            {
                Key = p.Key,
                Label = p.Label,
                Description = p.Description,
                Group = p.Group
            }).ToList(),
            DefaultUserPermissions = PermissionCatalog.DefaultUserPermissions,
            DefaultAdminPermissions = PermissionCatalog.DefaultAdminPermissions
        };

        return Ok(response);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUserPermissions()
    {
        var users = await _userRepository.GetAllAsync();
        
        // SuperAdmin'ler bu izin listesinde görünmez (SuperAdmin izinleri sabittir)
        var result = users
            .Where(u => u.Role != "SuperAdmin")
            .Select(u => new UserPermissionsDto
            {
                UserId = u.Id!,
                Username = u.Username,
                Role = u.Role,
                Permissions = u.Permissions ?? new List<string>()
            }).ToList();

        return Ok(result);
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUserPermissions(string id, [FromBody] UpdateUserPermissionsDto dto)
    {
        var validKeys = PermissionCatalog.All.Select(p => p.Key).ToHashSet();
        var filteredPermissions = dto.Permissions.Where(p => validKeys.Contains(p)).Distinct().ToList();

        var updated = await _userRepository.UpdatePermissionsAsync(id, filteredPermissions);
        if (!updated) return NotFound(new { Message = "User not found." });

        return Ok(new { Message = "Permissions updated successfully.", Permissions = filteredPermissions });
    }

    [HttpPost("users/{id}/apply-default")]
    public async Task<IActionResult> ApplyDefaultPermissions(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound(new { Message = "User not found." });

        var defaultSet = user.Role == "Admin"
            ? PermissionCatalog.DefaultAdminPermissions
            : PermissionCatalog.DefaultUserPermissions;

        await _userRepository.UpdatePermissionsAsync(id, defaultSet);
        return Ok(new { Message = "Default permissions applied.", Permissions = defaultSet });
    }
}
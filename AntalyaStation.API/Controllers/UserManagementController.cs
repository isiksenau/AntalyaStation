using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntalyaStation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// 🟢 Class-level Role kısıtlaması kaldırıldı — artık her action kendi Policy'sini
// (Permission.ManageUsers / Permission.SystemConsole) taşıyor. Class'ta Role olursa
// Policy'yi geçen ama Admin/SuperAdmin olmayan (permission verilmiş "User") kişiler
// yine de AND mantığıyla 403 alırdı.
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
    [Authorize(Policy = "Permission.ManageUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userRepository.GetAllAsync();

        // Filtreyi doğrudan sorgu akışına ekliyoruz
        var query = users.AsEnumerable();

        if (!User.IsInRole("SuperAdmin"))
        {
            query = query.Where(u => u.Role != "SuperAdmin");
        }

        var result = query.Select(u => new UserListItemDto
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
    [Authorize(Policy = "Permission.ManageUsers")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // 🟢 Rol Atama Kontrolü: Admin -> Sadece "User", SuperAdmin -> "Admin" veya "User"
        if (!CanAssignRole(dto.Role))
            return Forbid();

        var (success, error, user) = await _authService.CreateUserAsync(
            dto.Username, dto.Password, dto.FullName, dto.Email, dto.Role);

        if (!success)
            return BadRequest(new { Message = error });

        // 🟢 OTOMATİK İZİN ATAMA: Rolüne göre varsayılan izin setini atayalım
        var defaultPermissions = dto.Role == "Admin" 
            ? PermissionCatalog.DefaultAdminPermissions 
            : PermissionCatalog.DefaultUserPermissions;

        await _userRepository.UpdatePermissionsAsync(user!.Id!, defaultPermissions);

        return Ok(new { Message = "User created successfully.", UserId = user!.Id });
    }

    [HttpPut("{id}/role")]
    [Authorize(Policy = "Permission.ManageUsers")]

    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleDto dto)
    {
        var targetUser = await _userRepository.GetByIdAsync(id);
        if (targetUser == null) return NotFound(new { Message = "User not found." });

        // Normal Admin, SuperAdmin kullanıcısının rolünü değiştiremez
        if (targetUser.Role == "SuperAdmin" && !User.IsInRole("SuperAdmin"))
            return Forbid();

        if (!CanAssignRole(dto.Role))
            return Forbid();

        var updated = await _userRepository.UpdateRoleAsync(id, dto.Role);
        if (!updated) return NotFound(new { Message = "User not found." });

        return Ok(new { Message = "Role updated successfully." });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Permission.ManageUsers")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var targetUser = await _userRepository.GetByIdAsync(id);
        if (targetUser != null && targetUser.Role == "SuperAdmin" && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var deleted = await _userRepository.DeleteAsync(id);
        if (!deleted) return NotFound(new { Message = "User not found." });

        return Ok(new { Message = "User deleted successfully." });
    }

    [HttpGet("system-stats")]
    [Authorize(Policy = "Permission.SystemConsole")]  // 🟢 Bu endpoint ControlPanel.razor tarafından kullanılıyor, ManageUsers değil SystemConsole gerekiyor

    public async Task<IActionResult> GetSystemStats()
    {
        var stations = (await _stationRepository.GetAllAsync()).ToList();
        var totalUsers = await _userRepository.CountAllAsync();
        var adminCount = await _userRepository.CountByRoleAsync("Admin");
        var batches = await _excelImportService.GetActiveImportBatchesAsync();

        var stats = new SystemStatsDto
        {
            TotalStations = stations.Count,
            ActiveStations = stations.Count(s => s.Status != "Inactive"),
            InactiveStations = stations.Count(s => s.Status == "Inactive"),
            TotalUsers = (int)totalUsers,
            AdminCount = (int)adminCount,
            ActiveImportBatches = batches.Count,
            DatabaseName = "AntalyaStationDB",
            ApiVersion = "1.0.0"
        };

        return Ok(stats);
    }

    // 🟢 ROL ATAMA YETKİ KURALI
    private bool CanAssignRole(string? targetRole)
    {
        if (string.IsNullOrWhiteSpace(targetRole)) return false;

        // "SuperAdmin" rolü API üzerinden yeni kimseye atanamaz
        if (targetRole == "SuperAdmin") return false;

        // Normal Admin SADECE "User" oluşturabilir
        if (!User.IsInRole("SuperAdmin"))
        {
            return targetRole == "User";
        }

        // SuperAdmin hem "Admin" hem "User" oluşturabilir
        return targetRole == "User" || targetRole == "Admin";
    }
}
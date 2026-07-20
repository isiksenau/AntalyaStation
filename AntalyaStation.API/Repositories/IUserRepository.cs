using AntalyaStation.API.Models;

namespace AntalyaStation.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(string id);
    Task AddAsync(User user);
    Task<bool> UpdateProfileAsync(string id, string username, string fullName, string email, string phoneNumber);
    Task<bool> UpdatePasswordAsync(string id, string passwordHash, string passwordSalt);

    // 🟢 YENİ: Admin - User Management için
    Task<List<User>> GetAllAsync();
    Task<bool> UpdateRoleAsync(string id, string role);
    Task<bool> DeleteAsync(string id);
    Task<bool> IsUsernameTakenAsync(string username, string? excludeUserId = null);
    Task<bool> UpdatePermissionsAsync(string id, List<string> permissions);
    Task<long> CountAllAsync();
    Task<long> CountByRoleAsync(string role);
    
}
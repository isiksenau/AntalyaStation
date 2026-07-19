using AntalyaStation.API.Models;

namespace AntalyaStation.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(string id);
    Task AddAsync(User user);
    Task<bool> UpdateProfileAsync(string id, string fullName, string email);
    Task<bool> UpdatePasswordAsync(string id, string passwordHash, string passwordSalt);
}
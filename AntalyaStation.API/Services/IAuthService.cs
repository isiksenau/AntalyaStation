using AntalyaStation.API.Models;

namespace AntalyaStation.API.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string username, string password);

    // Creates the default "admin" account the first time the app runs, if it does not exist yet.
    Task<User> EnsureDefaultAdminAsync();

    (string Hash, string Salt) HashPassword(string password);
    bool VerifyPassword(string password, string hash, string salt);
}
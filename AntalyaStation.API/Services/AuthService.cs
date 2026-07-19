using System.Security.Cryptography;
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;

namespace AntalyaStation.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
 
        if (user == null)
        {
            Console.WriteLine($"[DEBUG] Kullanıcı bulunamadı: '{username}'");
            return null;
        }
 
        Console.WriteLine($"[DEBUG] Kullanıcı bulundu: '{user.Username}' (Id={user.Id})");
        Console.WriteLine($"[DEBUG] Stored Hash length={user.PasswordHash?.Length}, Salt length={user.PasswordSalt?.Length}");
 
        var isValid = VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
        Console.WriteLine($"[DEBUG] Şifre doğrulama sonucu: {isValid}");
 
        return isValid ? user : null;
    }

    public async Task<User> EnsureDefaultAdminAsync()
    {
        var existing = await _userRepository.GetByUsernameAsync("admin");
        if (existing != null) return existing;

        var (hash, salt) = HashPassword("Antalya123!");
        var admin = new User
        {
            Username = "admin",
            FullName = "System Administrator",
            Email = "admin@antalyastation.local",
            Role = "Admin",
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedDate = DateTime.UtcNow
        };

        await _userRepository.AddAsync(admin);
        return admin;
    }

    public (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt)) return false;

        var saltBytes = Convert.FromBase64String(salt);
        var expectedHash = Convert.FromBase64String(hash);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AntalyaStation.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AntalyaStation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto loginDto)
    {
        // 🧪 Şimdilik veritabanı karmaşasına girmemek için sabit bir kullanıcı yapalım.
        // İleride bu kullanıcıyı MongoDB'den sorgulayacak hale getirebiliriz.
        if (loginDto.Username == "admin" && loginDto.Password == "Antalya123!")
        {
            // Kullanıcı doğruysa ona özel bir Token üretelim:
            var token = GenerateJwtToken(loginDto.Username);
            
            return Ok(new { Token = token, Message = "Giriş Başarılı" });
        }

        // Kullanıcı adı veya şifre yanlışsa kapıyı kapat:
        return Unauthorized(new { Message = "Kullanıcı adı veya şifre hatalı!" });
    }

    private string GenerateJwtToken(string username)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Biletin içine yazılacak "Yolcu Bilgileri" (Claims)
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Her tokene benzersiz ID (İşte bu her seferinde değişimi garanti eder)
        };

        // Bilet basım aşaması (Zaman damgaları burada basılır)
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"]!)),
            signingCredentials: creds
        );

        // Bileti okunabilir şifreli bir metne dönüştür ve teslim et
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
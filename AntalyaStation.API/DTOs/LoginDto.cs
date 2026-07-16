namespace AntalyaStation.API.DTOs;

// 💡 Blazor'dan gelen kullanıcı adı ve şifreyi API'nin okuyabilmesi için bu özelliklerin birebir eşleşmesi şart!
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
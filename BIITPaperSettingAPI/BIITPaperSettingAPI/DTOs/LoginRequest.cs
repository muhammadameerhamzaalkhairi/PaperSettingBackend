using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MaxLength(1024)]
    public string Password { get; set; } = "";
}

public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

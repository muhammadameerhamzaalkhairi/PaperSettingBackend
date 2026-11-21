using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BIITPaperSettingAPI.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace BIITPaperSettingAPI.Services;

public class StaffTokens(IConfiguration config)
{
    public string Issuer { get; } = config["Jwt:Issuer"] ?? "BIIT.PaperSetting";
    public string Audience { get; } = config["Jwt:Audience"] ?? "BIIT.Staff";
    public SymmetricSecurityKey Key { get; } = ReadKey(config);

    private static SymmetricSecurityKey ReadKey(IConfiguration config)
    {
        var bytes = Convert.FromBase64String(config["Jwt:SigningKey"] ??
            throw new InvalidOperationException("Configure Jwt:SigningKey before startup."));
        if (bytes.Length < 32)
            throw new InvalidOperationException("JWT signing key requires at least 32 random bytes.");
        return new SymmetricSecurityKey(bytes);
    }

    public LoginResponse Issue(int actorId)
    {
        // Staff ID signed token ke andar jati hai aur token 30 minute baad expire hota hai.
        var expires = DateTime.UtcNow.AddMinutes(30);
        var token = new JwtSecurityToken(Issuer, Audience,
            [new Claim("sub", actorId.ToString())], expires: expires,
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

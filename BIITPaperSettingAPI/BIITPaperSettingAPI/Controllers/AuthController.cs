using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BIITPaperSettingAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(StaffAccessService staff, StaffTokens tokens) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellation)
    {
        var id = await staff.VerifyPasswordAsync(request, cancellation);
        if (id == null)
            return Unauthorized();
        if (await staff.GetCurrentAsync(id.Value, cancellation) == null)
            return Forbid();
        Response.Headers.CacheControl = "no-store";
        return Ok(tokens.Issue(id.Value));
    }
}

using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BIITPaperSettingAPI.Controllers;

[ApiController, Authorize]
[Route("api/exams")]
public class ExamsController(StaffAccessService staff) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<CurrentExamResponse>> Current(CancellationToken cancellation)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out int id) || id <= 0)
            return Unauthorized();
        var exam = await staff.GetCurrentAsync(id, cancellation);
        if (exam == null)
            return Forbid();
        return Ok(exam);
    }
}

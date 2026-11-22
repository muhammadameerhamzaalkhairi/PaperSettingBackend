using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[ApiController, Authorize]
public abstract class SetupController(SetupProcedures procedures) : ControllerBase
{
    // Signed token se ActorId lete hain. Request body se identity nahi lete.
    protected async Task<IActionResult> WriteAsync(string procedure, CancellationToken cancellation, params SqlParameter[] parameters)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out int actorId) || actorId <= 0)
            return Unauthorized();
        try
        {
            int? id = await procedures.WriteAsync(procedure, actorId, parameters, cancellation);
            if (id.HasValue)
                return Ok(new { id });
            return NoContent();
        }
        catch (SqlException error) when (error.Number == 51001) { return Forbid(); }
        catch (SqlException error) when (error.Number is 51010 or 51060) { return Conflict(new { message = error.Message }); }
        catch (SqlException error) when (error.Number is 2601 or 2627) { return Conflict("Duplicate record."); }
        catch (SqlException error) when (error.Number is 547 or 2628) { return BadRequest("Invalid reference or field value."); }
    }

    protected async Task<IActionResult> ReadAsync(string procedure, string[] names, CancellationToken cancellation)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out int actorId) || actorId <= 0)
            return Unauthorized();
        try
        {
            return Ok(await procedures.ReadAsync(procedure, actorId, names, cancellation));
        }
        catch (SqlException error) when (error.Number == 51001) { return Forbid(); }
    }
}

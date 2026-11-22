using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[ApiController, Authorize]
public abstract class SetupController(SetupProcedures procedures) : ControllerBase
{
    protected SetupProcedures Procedures { get; } = procedures;
    // Signed token se ActorId lete hain. Request body se identity nahi lete.
    protected async Task<IActionResult> WriteAsync(string procedure, CancellationToken cancellation, params SqlParameter[] parameters)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out int actorId) || actorId <= 0)
            return Unauthorized();
        try
        {
            int? id = await Procedures.WriteAsync(procedure, actorId, parameters, cancellation);
            if (id.HasValue)
                return Ok(new { id });
            return NoContent();
        }
        catch (SqlException error) when (error.Number == 51001) { return Forbid(); }
        catch (SqlException error) when (error.Number == 51040) { return NotFound(new { message = error.Message }); }
        catch (SqlException error) when (error.Number is 51010 or 51020 or 51060) { return Conflict(new { message = error.Message }); }
        catch (SqlException error) when (error.Number is 2601 or 2627) { return Conflict("Duplicate record."); }
        catch (SqlException error) when (error.Number is 547 or 2628) { return BadRequest("Invalid reference or field value."); }
    }

    protected async Task<IActionResult> ReadAsync(string procedure, string[] names, CancellationToken cancellation, params SqlParameter[] parameters)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out int actorId) || actorId <= 0)
            return Unauthorized();
        try
        {
            return Ok(await Procedures.ReadAsync(procedure, actorId, names, cancellation, parameters));
        }
        catch (SqlException error) when (error.Number is 51001 or 51002) { return Forbid(); }
        catch (SqlException error) when (error.Number == 51005 || error.Message.Contains("Only the setter can start a revision") ||
            (error.Number == 51031 && (error.Message.Contains("Not a paper contributor") || error.Message.Contains("Contributors add extra") || error.Message.Contains("cannot be edited by you")))) { return Forbid(); }
        catch (SqlException error) when (error.Number is 51003 or 51004 or 51006 ||
            (error.Number == 51031 && (error.Message.Contains("stale") || error.Message.Contains("latest draft") || error.Message.Contains("frozen") || error.Message.Contains("Copy question to a new revision")))) { return Conflict(new { message = error.Message }); }
        catch (SqlException error) when (error.Number == 51050) { return Conflict(new { message = error.Message }); }
        catch (SqlException error) when (error.Number == 51031) { return BadRequest(new { message = error.Message }); }
        catch (SqlException error) when (error.Number == 51020 && error.Message.Contains("not assigned to you")) { return Forbid(); }
        catch (SqlException error) when (error.Number == 51020 && error.Message.Contains("Invalid topic/status")) { return BadRequest(error.Message); }
        catch (SqlException error) when (error.Number is 51020 or 51030) { return Conflict(new { message = error.Message }); }
        catch (SqlException error) when (error.Number == 547) { return BadRequest("Invalid assignment or due date."); }
    }
}

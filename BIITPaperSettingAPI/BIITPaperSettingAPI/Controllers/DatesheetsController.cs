using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[ApiController, Authorize, Route("api/datesheets")]
public class DatesheetsController(SetupProcedures procedures) : ControllerBase
{
    [HttpGet("cycles/{cycleId:int}/courses")]
    public Task<IActionResult> Courses([Range(1,int.MaxValue)] int cycleId, CancellationToken cancellation)
        => Run("dbo.usp_GetDatesheetCourses", ["courses"], cancellation, new SqlParameter("@ExamCycleId", cycleId));

    // =====================================================
    // STEP 1: VALIDATE EXCEL AND SAVE A DRAFT
    // =====================================================
    // Pehle Data Cell access check hota hai. Ghalat rows hon to draft save nahi hota.
    [HttpPost("cycles/{cycleId:int}/drafts"), RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Upload([Range(1,int.MaxValue)] int cycleId, IFormFile file, CancellationToken cancellation)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out int actor))
            return Unauthorized();
        try
        {
            var scope = await procedures.ReadAsync("dbo.usp_GetDatesheetCourses", actor, ["courses"], cancellation,
                new SqlParameter("@ExamCycleId", cycleId));
            if (file.Length == 0 || file.Length > 2 * 1024 * 1024 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Upload a nonempty .xlsx workbook of up to 2 MiB.");
            var allowed = scope["courses"].Select(row => (int)row["course_exam_id"]!).ToHashSet();
            using var stream = file.OpenReadStream();
            DatesheetImport import;
            try { import = DatesheetWorkbook.Read(stream, allowed, cancellation); }
            catch (Exception error) when (error is InvalidDataException or FormatException or ArgumentException or System.Xml.XmlException or DocumentFormat.OpenXml.Packaging.OpenXmlPackageException or System.IO.FileFormatException or NotSupportedException)
            {
                return BadRequest("Unreadable workbook. Use one .xlsx sheet with four columns and up to 5000 data rows.");
            }
            if (import.Errors.Count > 0)
                return UnprocessableEntity(new { import.Errors });
            return await Run("dbo.usp_SaveDatesheetDraft", ["draft"], cancellation,
                new SqlParameter("@ExamCycleId", cycleId),
                new SqlParameter("@ScheduleJson", SqlDbType.NVarChar, -1) {Value = JsonSerializer.Serialize(import.Schedule)});
        }
        catch (SqlException error) when (error.Number == 51001) { return Forbid(); }
        catch (SqlException error) when (error.Number == 51021) { return BadRequest(error.Message); }
    }

    [HttpGet("{id:int}/preview")]
    public Task<IActionResult> Preview([Range(1,int.MaxValue)] int id, CancellationToken cancellation)
        => Run("dbo.usp_GetDatesheetPreview", ["header","schedule","errors"], cancellation, new SqlParameter("@DatesheetId", id));

    // Sirf is explicit action se sab roles ka current exam badalta hai.
    [HttpPost("{id:int}/publish")]
    public Task<IActionResult> Publish([Range(1,int.MaxValue)] int id, CancellationToken cancellation)
        => Run("dbo.usp_PublishDatesheet", ["current"], cancellation, new SqlParameter("@DatesheetId", id));

    [HttpGet("history")]
    public Task<IActionResult> History(CancellationToken cancellation)
        => Run("dbo.usp_GetDatesheetHistory", ["history"], cancellation);

    private async Task<IActionResult> Run(string procedure, string[] names, CancellationToken cancellation, params SqlParameter[] inputs)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out int actor))
            return Unauthorized();
        try
        {
            var data = await procedures.ReadAsync(procedure, actor, names, cancellation, inputs);
            if (data.TryGetValue("header", out var header) && header.Count == 0)
                return NotFound();
            return Ok(data);
        }
        catch (SqlException error) when (error.Number == 51001) { return Forbid(); }
        catch (SqlException error) when (error.Number == 51021) { return Conflict(new {message=error.Message}); }
    }
}

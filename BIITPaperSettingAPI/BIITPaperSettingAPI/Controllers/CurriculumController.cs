using System.ComponentModel.DataAnnotations;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api/curriculum")]
public class CurriculumController(SetupProcedures procedures) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: CURRICULUM SETUP
    // =====================================================
    // SQL procedure staff ka role check karke apni transaction chalati hai.

    [HttpPost("outlines")]
    public async Task<IActionResult> CreateOutline(CreateOutlineRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_CreateOutline", cancellation,
            new SqlParameter("@CourseId", request.CourseId),
            new SqlParameter("@VersionNumber", request.VersionNumber));
    }

    [HttpPost("topics")]
    public async Task<IActionResult> AddOutlineTopic(AddOutlineTopicRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddOutlineTopic", cancellation,
            new SqlParameter("@OutlineId", request.OutlineId),
            new SqlParameter("@Title", request.Title),
            new SqlParameter("@ParentTopicId", (object?)request.ParentTopicId ?? DBNull.Value),
            new SqlParameter("@Week", (object?)request.Week ?? DBNull.Value),
            new SqlParameter("@SortOrder", request.SortOrder));
    }

    [HttpPost("clos")]
    public async Task<IActionResult> AddOutlineCLO(AddOutlineCLORequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddOutlineCLO", cancellation,
            new SqlParameter("@OutlineId", request.OutlineId),
            new SqlParameter("@Code", request.Code),
            new SqlParameter("@Statement", request.Statement));
    }

    [HttpPut("topics/{id:int}")]
    public async Task<IActionResult> UpdateOutlineTopic([Range(1, int.MaxValue)] int id, UpdateOutlineTopicRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_UpdateOutlineTopic", cancellation,
            new SqlParameter("@TopicId", id),
            new SqlParameter("@Title", request.Title),
            new SqlParameter("@Week", (object?)request.Week ?? DBNull.Value),
            new SqlParameter("@SortOrder", request.SortOrder));
    }

    [HttpPut("clos/{id:int}")]
    public async Task<IActionResult> UpdateOutlineCLO([Range(1, int.MaxValue)] int id, UpdateOutlineCLORequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_UpdateOutlineCLO", cancellation,
            new SqlParameter("@CloId", id),
            new SqlParameter("@Code", request.Code),
            new SqlParameter("@Statement", request.Statement));
    }

    [HttpPost("outlines/{id:int}/publish")]
    public async Task<IActionResult> PublishOutline([Range(1, int.MaxValue)] int id, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_PublishOutline", cancellation,
            new SqlParameter("@OutlineId", id));
    }

    [HttpPost("exams")]
    public async Task<IActionResult> ConfigureCourseExam(ConfigureCourseExamRequest request, CancellationToken cancellation)
    {
        if (decimal.Round(request.Marks, 2) != request.Marks)
            return BadRequest("Invalid ConfigureCourseExam request.");
        return await WriteAsync("dbo.usp_ConfigureCourseExam", cancellation,
            new SqlParameter("@OfferingId", request.OfferingId),
            new SqlParameter("@ExamCycleId", request.ExamCycleId),
            new SqlParameter("@Marks", request.Marks),
            new SqlParameter("@Duration", request.Duration));
    }

    [HttpPut("exams/{id:int}")]
    public async Task<IActionResult> UpdateCourseExamSettings([Range(1, int.MaxValue)] int id, UpdateCourseExamSettingsRequest request, CancellationToken cancellation)
    {
        if (decimal.Round(request.Marks, 2) != request.Marks)
            return BadRequest("Invalid UpdateCourseExamSettings request.");
        return await WriteAsync("dbo.usp_UpdateCourseExamSettings", cancellation,
            new SqlParameter("@ExamId", id),
            new SqlParameter("@Marks", request.Marks),
            new SqlParameter("@Duration", request.Duration));
    }

    [HttpPut("exams/{id:int}/clos")]
    public async Task<IActionResult> SetExamCLOs([Range(1, int.MaxValue)] int id, SetExamCLOsRequest request, CancellationToken cancellation)
    {
        if (request.CloIds.Any(id => id <= 0) || request.CloIds.Distinct().Count() != request.CloIds.Count)
            return BadRequest("Invalid SetExamCLOs request.");
        return await WriteAsync("dbo.usp_SetExamCLOs", cancellation,
            new SqlParameter("@ExamId", id),
            new SqlParameter("@CloIdsJson", System.Text.Json.JsonSerializer.Serialize(request.CloIds)));
    }
}

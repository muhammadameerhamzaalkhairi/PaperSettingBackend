using System.ComponentModel.DataAnnotations;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api/academic")]
public class AcademicController(SetupProcedures procedures) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: ACADEMIC SETUP
    // =====================================================
    // SQL procedure staff ka role check karke apni transaction chalati hai.

    [HttpPost("departments")]
    public async Task<IActionResult> AddDepartment(AddDepartmentRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddDepartment", cancellation,
            new SqlParameter("@department_code", request.Code),
            new SqlParameter("@department_name", request.Name));
    }

    [HttpPost("programs")]
    public async Task<IActionResult> AddProgram(AddProgramRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddProgram", cancellation,
            new SqlParameter("@department_id", (object?)request.DepartmentId ?? DBNull.Value),
            new SqlParameter("@program_code", request.Code),
            new SqlParameter("@program_name", request.Name));
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> AddSession(AddSessionRequest request, CancellationToken cancellation)
    {
        if (request.EndsOn < request.StartsOn)
            return BadRequest("Invalid AddSession request.");
        return await WriteAsync("dbo.usp_AddSession", cancellation,
            new SqlParameter("@session_name", request.Name),
            new SqlParameter("@starts_on", request.StartsOn!.Value.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@ends_on", request.EndsOn!.Value.ToDateTime(TimeOnly.MinValue)));
    }

    [HttpPost("sections")]
    public async Task<IActionResult> AddSection(AddSectionRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddSection", cancellation,
            new SqlParameter("@program_id", request.ProgramId),
            new SqlParameter("@session_id", request.SessionId),
            new SqlParameter("@section_name", request.Name),
            new SqlParameter("@semester_number", request.SemesterNumber));
    }

    [HttpPost("courses")]
    public async Task<IActionResult> AddCourse(AddCourseRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddCourse", cancellation,
            new SqlParameter("@owner_department_id", (object?)request.DepartmentId ?? DBNull.Value),
            new SqlParameter("@course_code", request.Code),
            new SqlParameter("@course_title", request.Title));
    }

    [HttpPost("exam-cycles")]
    public async Task<IActionResult> AddExamCycle(AddExamCycleRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddExamCycle", cancellation,
            new SqlParameter("@session_id", request.SessionId),
            new SqlParameter("@exam_type", request.ExamType));
    }

    [HttpPost("offerings")]
    public async Task<IActionResult> AddCourseOffering(AddCourseOfferingRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddCourseOffering", cancellation,
            new SqlParameter("@CourseId", request.CourseId),
            new SqlParameter("@OutlineId", request.OutlineId),
            new SqlParameter("@SessionId", request.SessionId),
            new SqlParameter("@DepartmentId", (object?)request.DepartmentId ?? DBNull.Value));
    }

    [HttpPost("offering-sections")]
    public async Task<IActionResult> AddOfferingSection(AddOfferingSectionRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AddOfferingSection", cancellation,
            new SqlParameter("@OfferingId", request.OfferingId),
            new SqlParameter("@SectionId", request.SectionId));
    }

    [HttpPost("teaching-assignments")]
    public async Task<IActionResult> AssignTeaching(AssignTeachingRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_AssignTeaching", cancellation,
            new SqlParameter("@OfferingSectionId", request.OfferingSectionId),
            new SqlParameter("@TeacherId", request.TeacherId));
    }

    [HttpPost("teaching-assignments/{id:int}/end")]
    public async Task<IActionResult> EndTeachingAssignment([Range(1, int.MaxValue)] int id, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_EndTeachingAssignment", cancellation,
            new SqlParameter("@TeachingAssignmentId", id));
    }

    [HttpGet("lookups")]
    public Task<IActionResult> GetAcademicLookups(CancellationToken cancellation)
    {
        return ReadAsync("dbo.usp_GetAcademicLookups", ["departments", "programs", "sessions", "examCycles", "courses", "sections", "outlines", "staff"], cancellation);
    }
}

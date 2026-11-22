using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api/exams/{examId:int}")]
public class CoverageController(SetupProcedures procedures) : SetupController(procedures)
{
    [HttpGet("coverage")]
    public Task<IActionResult> GetCoverage([Range(1,int.MaxValue)] int examId, CancellationToken cancellation)
    {
        return ReadAsync("dbo.usp_GetCoverageWorkspace",
            ["sections","matrix","commonTopics","eligibleClos","scope","lockedTopics","setterHistory"],
            cancellation, new SqlParameter("@ExamId", examId));
    }

    // =====================================================
    // STEP 1: SAVE ONE TEACHER'S SECTION COVERAGE
    // =====================================================
    // SQL sirf signed-in teacher ki apni assignment ki selection replace karti hai.
    [HttpPut("coverage/assignments/{assignmentId:int}")]
    public async Task<IActionResult> Save([Range(1,int.MaxValue)] int examId, [Range(1,int.MaxValue)] int assignmentId,
        SectionCoverageRequest request, CancellationToken cancellation)
    {
        var seen = new HashSet<int>();
        var topics = new List<object>();
        foreach (var topic in request.Topics!)
        {
            if (topic == null || !seen.Add(topic.TopicId))
                return BadRequest("Supply each topic exactly once.");
            topics.Add(new { topic_id = topic.TopicId, status = topic.Status });
        }
        return await ReadAsync("dbo.usp_SaveCoverage", ["sections"], cancellation,
            new SqlParameter("@ExamId", examId), new SqlParameter("@TeachingAssignmentId", assignmentId),
            new SqlParameter("@TopicsJson", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(topics) },
            new SqlParameter("@Submit", request.Submit));
    }

    // =====================================================
    // STEP 2: LOCK COMMON TOPICS AND ASSIGN A SETTER
    // =====================================================
    // Department aur setter eligibility procedures dobara verify karti hain.
    [HttpPost("coverage/lock")]
    public Task<IActionResult> Lock([Range(1,int.MaxValue)] int examId, CancellationToken cancellation)
    {
        return WriteAsync("dbo.usp_LockCommonTopics", cancellation, new SqlParameter("@ExamId", examId));
    }

    [HttpPost("setter-assignments")]
    public async Task<IActionResult> Assign([Range(1,int.MaxValue)] int examId, SetterAssignmentRequest request,
        CancellationToken cancellation)
    {
        if (request.DueAt <= DateTimeOffset.UtcNow)
            return BadRequest("Choose a future due date.");
        return await ReadAsync("dbo.usp_AssignPaperSetter", ["paper"], cancellation,
            new SqlParameter("@ExamId", examId), new SqlParameter("@TeacherId", request.TeacherId),
            new SqlParameter("@DueAt", SqlDbType.DateTime2) { Value = request.DueAt!.Value.UtcDateTime });
    }
}

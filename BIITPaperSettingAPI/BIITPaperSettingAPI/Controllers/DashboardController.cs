using System.ComponentModel.DataAnnotations;
using System.Data;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api")]
public class DashboardController(SetupProcedures procedures) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: READ THE CURRENT OR SELECTED EXAM
    // =====================================================
    // Filter sirf is request ke liye hai. Current exam SQL mein publish karne se badalta hai.
    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard([Range(1,int.MaxValue)] int? examCycleId,CancellationToken cancellation)
        => ReadAsync("dbo.usp_GetDashboard",["exam","statuses","notifications"],cancellation,Cycle(examCycleId));

    [HttpGet("courses")]
    public Task<IActionResult> Courses([Range(1,int.MaxValue)] int? examCycleId,CancellationToken cancellation)
        => ReadAsync("dbo.usp_ListMyCourses",["courses"],cancellation,Cycle(examCycleId));

    [HttpGet("papers")]
    public Task<IActionResult> Papers([Range(1,int.MaxValue)] int? examCycleId,[StringLength(40)] string? status,
        [StringLength(200)] string? search,CancellationToken cancellation,[Range(1,int.MaxValue)] int page=1,
        [Range(1,100)] int pageSize=20)
    {
        if ((long)(page-1)*pageSize > int.MaxValue) return Task.FromResult<IActionResult>(BadRequest("Page offset is too large."));
        return ReadAsync("dbo.usp_ListPapers",["papers"],cancellation,Cycle(examCycleId),
            new SqlParameter("@Status",SqlDbType.NVarChar,40){Value=(object?)status ?? DBNull.Value},
            new SqlParameter("@Search",SqlDbType.NVarChar,200){Value=(object?)search ?? DBNull.Value},
            new SqlParameter("@Page",page),new SqlParameter("@PageSize",pageSize));
    }

    private static SqlParameter Cycle(int? id) => new("@ExamCycleId",SqlDbType.Int){Value=(object?)id ?? DBNull.Value};

    // =====================================================
    // STEP 2: COMMENT ON A REVISION OR ARCHIVE A RECEIVED PAPER
    // =====================================================
    [HttpPost("paper-versions/{versionId:int:min(1)}/comments")]
    public Task<IActionResult> Comment(int versionId,PaperCommentRequest request,CancellationToken cancellation)
        => WriteAsync("dbo.usp_AddComment",cancellation,new SqlParameter("@VersionId",versionId),
            new SqlParameter("@Text",SqlDbType.NVarChar,-1){Value=request.Text.Trim()},
            new SqlParameter("@PaperItemId",SqlDbType.Int){Value=(object?)request.PaperItemId ?? DBNull.Value},
            new SqlParameter("@ParentCommentId",SqlDbType.Int){Value=(object?)request.ParentCommentId ?? DBNull.Value});

    [HttpPost("papers/{paperId:int:min(1)}/archive")]
    public Task<IActionResult> Archive(int paperId,CancellationToken cancellation)
        => WriteAsync("dbo.usp_ArchivePaper",cancellation,new SqlParameter("@PaperId",paperId));

    [HttpGet("imports/history")]
    public Task<IActionResult> Imports([Range(1,int.MaxValue)] int? importId,CancellationToken cancellation)
        => ReadAsync("dbo.usp_GetImportHistory",["batches","errors"],cancellation,
            new SqlParameter("@ImportId",SqlDbType.Int){Value=(object?)importId ?? DBNull.Value});
}

using System.Data;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api")]
public class PaperReviewController(SetupProcedures procedures) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: SUBMIT THE SETTER'S EXACT DRAFT
    // =====================================================
    [HttpPost("paper-versions/{versionId:int:min(1)}/submit")]
    public Task<IActionResult> Submit(int versionId, DraftChange request, CancellationToken cancellation)
        => ReadAsync("dbo.usp_SubmitPaper", ["submission"], cancellation,
            new SqlParameter("@VersionId", versionId), new SqlParameter("@ExpectedRowVersion", request.RowVersion));

    // =====================================================
    // STEP 2: RECORD A DECISION ON A FROZEN REVISION
    // =====================================================
    // Client ko dono IDs deni hain taake purani screen se naya paper approve na ho.
    [HttpPost("submissions/{submissionId:int:min(1)}/versions/{versionId:int:min(1)}/decisions")]
    public async Task<IActionResult> Decide(int submissionId, int versionId, PaperDecisionRequest request, CancellationToken cancellation)
    {
        if (request.Decision == "CHANGES_REQUESTED" && string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Request Changes requires a reason.");

        return await ReadAsync("dbo.usp_ReviewPaper", ["review"], cancellation,
            new SqlParameter("@SubmissionId", submissionId), new SqlParameter("@ReviewedVersionId", versionId),
            new SqlParameter("@Stage", request.Stage), new SqlParameter("@Decision", request.Decision),
            new SqlParameter("@Feedback", SqlDbType.NVarChar, -1) {Value=(object?)request.Reason ?? DBNull.Value},
            new SqlParameter("@DelegationId", SqlDbType.Int) {Value=(object?)request.DelegationId ?? DBNull.Value});
    }

    [HttpGet("paper-review/actions")]
    public IActionResult Actions() => Ok(new[] {
        new { decision = "APPROVED", label = "Approve" },
        new { decision = "CHANGES_REQUESTED", label = "Request Changes" }
    });
}

using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api")]
public class CheckingController(SetupProcedures procedures) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: RECORD THE HOD DECISION FOR ONE EXAM CLO
    // =====================================================
    // Mapping ID sawal ki khaas revision aur exam CLO dono ko pehchanti hai.
    [HttpPost("clo-mappings/{mappingId:int:min(1)}/reviews")]
    public Task<IActionResult> Review(int mappingId, CloReviewRequest request, CancellationToken cancellation)
        => WriteAsync("dbo.usp_ReviewCLOMapping", cancellation, new SqlParameter("@MappingId", mappingId),
            new SqlParameter("@Status", request.Status), new SqlParameter("@Explanation", request.Explanation.Trim()));

    // =====================================================
    // STEP 2: CHECK EXACT REUSE AND READ A RUN
    // =====================================================
    // Har purane paper ka apna percentage aur matching sawalon ki tafseel milti hai.
    [HttpPost("paper-versions/{versionId:int:min(1)}/similarity/exact")]
    public Task<IActionResult> RunExact(int versionId, CancellationToken cancellation)
        => ReadAsync("dbo.usp_RunExactSimilarity", ["run"], cancellation, new SqlParameter("@VersionId", versionId));

    [HttpGet("similarity-runs/{runId:int:min(1)}")]
    public Task<IActionResult> Report(int runId, CancellationToken cancellation)
        => ReadAsync("dbo.usp_GetSimilarityReport", ["run", "sources", "matches"], cancellation,
            new SqlParameter("@RunId", runId));

    [HttpGet("checking/semantic")]
    public IActionResult SemanticAvailability()
        => Ok(new { mode = "SEMANTIC", status = "DEFERRED" });
}

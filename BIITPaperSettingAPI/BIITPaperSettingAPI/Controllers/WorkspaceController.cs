using System.Data;
using System.Text.Json;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api")]
public class WorkspaceController(SetupProcedures procedures) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: OPEN A REVISION OR READ ITS HISTORY
    // =====================================================
    // Har revision ki apni ID aur concurrency token hoti hai.
    [HttpPost("papers/{paperId:int:min(1)}/versions")]
    public Task<IActionResult> Start(int paperId, RevisionRequest request, CancellationToken cancellation)
        => ReadAsync("dbo.usp_StartPaperRevision", ["version"], cancellation,
            new SqlParameter("@PaperId", paperId), new SqlParameter("@Reason", request.Reason));

    [HttpGet("papers/{paperId:int:min(1)}/versions")]
    public Task<IActionResult> History(int paperId, CancellationToken cancellation)
        => ReadAsync("dbo.usp_GetPaperVersions", ["versions","reviews"], cancellation, new SqlParameter("@PaperId", paperId));

    [HttpGet("paper-versions/{versionId:int:min(1)}")]
    public Task<IActionResult> Workspace(int versionId, CancellationToken cancellation)
        => ReadAsync("dbo.usp_GetPaperWorkspace", ["header","questions","parts","cloMappings","comments","sections"],
            cancellation, new SqlParameter("@VersionId", versionId));

    [HttpGet("question-versions/{questionVersionId:int:min(1)}/history")]
    public Task<IActionResult> QuestionHistory(int questionVersionId, CancellationToken cancellation)
        => ReadAsync("dbo.usp_GetQuestionRevisionHistory", ["revisions","parts","cloMappings"], cancellation,
            new SqlParameter("@QuestionVersionId",questionVersionId));

    // =====================================================
    // STEP 2: SAVE A QUESTION AS A NEW QUESTION REVISION
    // =====================================================
    [HttpPost("paper-versions/{versionId:int:min(1)}/questions")]
    public Task<IActionResult> Add(int versionId, QuestionRequest request, CancellationToken cancellation)
        => Save(versionId, null, request, cancellation);

    [HttpPut("paper-versions/{versionId:int:min(1)}/items/{itemId:int:min(1)}/question")]
    public Task<IActionResult> Edit(int versionId, int itemId, QuestionRequest request, CancellationToken cancellation)
        => Save(versionId, itemId, request, cancellation);

    // Director ki edit asal submission se judi alag frozen revision banati hai.
    [HttpPost("submissions/{submissionId:int:min(1)}/versions/{versionId:int:min(1)}/items/{itemId:int:min(1)}/revisions")]
    public Task<IActionResult> DirectorEdit(int submissionId, int versionId, int itemId, DirectorEditRequest request, CancellationToken cancellation)
        => Save(versionId, itemId, request, cancellation, submissionId, request.Reason, request.DelegationId);

    private async Task<IActionResult> Save(int versionId, int? itemId, QuestionRequest request, CancellationToken cancellation,
        int? submissionId = null, string? reason = null, int? delegationId = null)
    {
        var keys = new HashSet<int>();
        foreach (var part in request.Parts)
        {
            if (part == null || !keys.Add(part.Key))
                return BadRequest("Each part needs a unique key.");
            if (part.Kind == "GROUP" && (part.Marks != null || part.TopicId != null || part.Difficulty != null))
                return BadRequest("Groups cannot carry marks, topic or difficulty.");
            if (part.Kind == "ITEM" && (part.TopicId == null || part.Difficulty == null || part.Marks is null or <= 0 or > 99999999.99m
                || decimal.Round(part.Marks.Value, 2) != part.Marks))
                return BadRequest("Each scored item needs a topic, difficulty and positive marks with at most two decimal places.");
        }
        var mappings = new HashSet<(int?,int)>();
        foreach (var mapping in request.Mappings)
        {
            if (mapping == null || !mappings.Add((mapping.PartKey, mapping.ExamCloId)))
                return BadRequest("Each part/CLO mapping must be unique.");
        }
        var parts = request.Parts.Select(p => new { k=p.Key,parent_k=p.ParentKey,kind=p.Kind,label=p.Label,
            body=p.Body,topic=p.TopicId,marks=p.Marks,difficulty=p.Difficulty });
        var clos = request.Mappings.Select(m => new { k=m.PartKey,exam_clo_id=m.ExamCloId });
        return await ReadAsync("dbo.usp_SaveQuestion", ["saved"], cancellation,
            new SqlParameter("@VersionId",versionId), Token(request), new SqlParameter("@Prompt",request.Prompt),
            new SqlParameter("@Difficulty",request.Difficulty), new SqlParameter("@PartsJson",JsonSerializer.Serialize(parts)),
            new SqlParameter("@CloMappingsJson",JsonSerializer.Serialize(clos)), new SqlParameter("@Placement",request.Placement),
            new SqlParameter("@PaperItemId", SqlDbType.Int) {Value=(object?)itemId ?? DBNull.Value},
            new SqlParameter("@ReviewSubmissionId", SqlDbType.Int) {Value=(object?)submissionId ?? DBNull.Value},
            new SqlParameter("@EditReason", SqlDbType.NVarChar, 500) {Value=(object?)reason ?? DBNull.Value},
            new SqlParameter("@DelegationId", SqlDbType.Int) {Value=(object?)delegationId ?? DBNull.Value});
    }

    // =====================================================
    // STEP 3: SETTER ORGANIZES THE TWO TABS
    // =====================================================
    [HttpPut("paper-versions/{versionId:int:min(1)}/items/{itemId:int:min(1)}/placement")]
    public Task<IActionResult> Move(int versionId, int itemId, PlacementRequest request, CancellationToken cancellation)
        => ReadAsync("dbo.usp_MovePaperItem", ["saved"], cancellation, new SqlParameter("@VersionId",versionId),
            Token(request), new SqlParameter("@PaperItemId",itemId), new SqlParameter("@Placement",request.Placement));

    [HttpPut("paper-versions/{versionId:int:min(1)}/order")]
    public async Task<IActionResult> Reorder(int versionId, ReorderRequest request, CancellationToken cancellation)
    {
        var ids = request.ItemIds!;
        if (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            return BadRequest("Supply each item ID once.");
        return await ReadAsync("dbo.usp_ReorderPaperItems", ["saved"], cancellation, new SqlParameter("@VersionId",versionId),
            Token(request), new SqlParameter("@Placement",request.Placement), new SqlParameter("@OrderJson",JsonSerializer.Serialize(ids)));
    }

    [HttpPut("paper-versions/{versionId:int:min(1)}/instructions")]
    public Task<IActionResult> Instructions(int versionId, InstructionsRequest request, CancellationToken cancellation)
        => ReadAsync("dbo.usp_SavePaperInstructions", ["saved"], cancellation, new SqlParameter("@VersionId",versionId),
            Token(request), new SqlParameter("@Instructions",SqlDbType.NVarChar,-1) {Value=(object?)request.Instructions ?? DBNull.Value});

    private static SqlParameter Token(DraftChange request)
        => new SqlParameter("@ExpectedRowVersion",SqlDbType.Binary,8) {Value=request.RowVersion};
}

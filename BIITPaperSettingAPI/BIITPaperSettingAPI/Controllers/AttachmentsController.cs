using System.ComponentModel.DataAnnotations;
using System.Data;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api/paper-versions/{versionId:int:min(1)}/attachments")]
public class AttachmentsController(SetupProcedures procedures, PrivateDiagrams diagrams) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: ATTACH A FILE TO THE EXACT DRAFT REVISION
    // =====================================================
    [HttpPost, RequestSizeLimit(6*1024*1024)]
    public async Task<IActionResult> Add(int versionId, [FromForm] DiagramUpload form, CancellationToken cancellation)
    {
        var file = await diagrams.StoreAsync(form.File, cancellation);
        if (file == null)
            return BadRequest("Upload a PNG or JPEG diagram up to 5 MiB.");
        // SQL setter, revision, part aur token check karke metadata aur link aik saath save karti hai.
        var result = await ReadAsync("dbo.usp_AttachDraftFile", ["saved"], cancellation,
            new SqlParameter("@VersionId",versionId), new SqlParameter("@ExpectedRowVersion",SqlDbType.Binary,8){Value=form.RowVersion},
            new SqlParameter("@QuestionVersionId",form.QuestionVersionId),new SqlParameter("@PartId",SqlDbType.Int){Value=(object?)form.PartId ?? DBNull.Value},
            new SqlParameter("@DisplayOrder",form.DisplayOrder),new SqlParameter("@StorageKey",file.Key),
            new SqlParameter("@OriginalName",file.Name),new SqlParameter("@MimeType",file.MimeType),new SqlParameter("@Checksum",file.Checksum));
        if (result is not OkObjectResult)
            diagrams.Delete(file.Key);
        return result;
    }

    [HttpDelete("{attachmentId:int:min(1)}")]
    public Task<IActionResult> Remove(int versionId,int attachmentId,[FromBody] DraftChange request,CancellationToken cancellation)
        => ReadAsync("dbo.usp_RemoveQuestionAttachment",["saved"],cancellation,new SqlParameter("@VersionId",versionId),
            new SqlParameter("@ExpectedRowVersion",SqlDbType.Binary,8){Value=request.RowVersion},new SqlParameter("@AttachmentId",attachmentId));

    [HttpGet]
    public Task<IActionResult> List(int versionId,CancellationToken cancellation) => ReadFiles(versionId,null,cancellation);

    [HttpGet("{attachmentId:int:min(1)}")]
    public Task<IActionResult> Download(int versionId,int attachmentId,CancellationToken cancellation)
        => ReadFiles(versionId,attachmentId,cancellation);

    private async Task<IActionResult> ReadFiles(int versionId,int? attachmentId,CancellationToken cancellation)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value,out int actor))
            return Unauthorized();
        try
        {
            var result = await Procedures.ReadAsync("dbo.usp_GetQuestionAttachments",actor,["attachments"],cancellation,
                new SqlParameter("@VersionId",versionId));
            var files = result["attachments"];
            foreach (var file in files)
            {
                string key = (string)file["storage_key"]!;
                file.Remove("storage_key");
                if (attachmentId == (int)file["attachment_id"]!)
                {
                    var stream = diagrams.Open(key);
                    if (stream == null) return NotFound();
                    Response.Headers.CacheControl = "no-store";
                    Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return File(stream,(string)file["mime_type"]!,(string)file["original_name"]!);
                }
            }
            if (attachmentId.HasValue) return NotFound();
            return Ok(result);
        }
        catch (SqlException error) when (error.Number is 51001 or 51002) { return Forbid(); }
    }
}

public class DiagramUpload : DraftChange
{
    [Required] public IFormFile File { get; set; } = null!;
    [Range(1,int.MaxValue)] public int QuestionVersionId { get; set; }
    [Range(1,int.MaxValue)] public int? PartId { get; set; }
    [Range(1,int.MaxValue)] public int DisplayOrder { get; set; } = 1;
}

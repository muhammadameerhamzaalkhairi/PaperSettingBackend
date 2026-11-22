using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api/approvals/{approvalId:int:min(1)}")]
public class PrintController(SetupProcedures procedures, ApprovedPaperPdf renderer, PrivateDiagrams files) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: GENERATE AND RECORD THE APPROVED PDF
    // =====================================================
    [HttpPost("print-exports")]
    public async Task<IActionResult> Export(int approvalId, CancellationToken cancellation)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value,out int actor)) return Unauthorized();
        try
        {
            var content = await Procedures.ReadAsync("dbo.usp_GetApprovedPrintContent",actor,
                ["header","questions","parts","diagrams"],cancellation,new SqlParameter("@ApprovalReviewId",approvalId));
            byte[] bytes = await renderer.RenderAsync(content,cancellation);
            string key = await files.StoreBytesAsync(bytes,cancellation);
            int? export;
            try
            {
                export = await Procedures.WriteAsync("dbo.usp_RecordPrintExport",actor,
                [new SqlParameter("@ApprovalReviewId",approvalId),new SqlParameter("@StorageKey",key),
                 new SqlParameter("@Checksum",Convert.ToHexString(SHA256.HashData(bytes)))],cancellation);
            }
            catch (SqlException error) when (error.Number is 51001 or 51002 or 51040)
            {
                files.Delete(key);
                throw;
            }
            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Print-Export-Id"] = export.ToString();
            return File(bytes,"application/pdf",$"exam-{approvalId}.pdf");
        }
        catch (SqlException error) when (error.Number is 51001 or 51002) { return Forbid(); }
        catch (SqlException error) when (error.Number == 51040) { return Conflict(new {message=error.Message}); }
        catch (InvalidDataException) { return Conflict("The approved diagram could not be verified."); }
    }

    // =====================================================
    // STEP 2: DOWNLOAD AN EXISTING PRINT EVENT
    // =====================================================
    [HttpGet("print-exports/{exportId:int:min(1)}")]
    public async Task<IActionResult> Download(int approvalId, int exportId, CancellationToken cancellation)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value,out int actor)) return Unauthorized();
        try
        {
            var result = await Procedures.ReadAsync("dbo.usp_GetPrintExport",actor,["file"],cancellation,
                new SqlParameter("@ApprovalReviewId",approvalId),new SqlParameter("@ExportId",exportId));
            if (result["file"].Count == 0) return NotFound();
            Response.Headers.CacheControl = "no-store";
            var file = result["file"][0];
            var stream = files.Open((string)file["storage_key"]!);
            if (stream == null) return NotFound();
            try
            {
                byte[] hash = await SHA256.HashDataAsync(stream,cancellation);
                if (Convert.ToHexString(hash) != (string)file["checksum"]!)
                {
                    stream.Dispose();
                    return Conflict("Stored PDF checksum changed.");
                }
                stream.Position = 0;
                return File(stream,"application/pdf",$"exam-{approvalId}.pdf");
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
        catch (SqlException error) when (error.Number is 51001 or 51002) { return Forbid(); }
    }

    // =====================================================
    // STEP 3: RECORD PHYSICAL HANDOVER
    // =====================================================
    // Sirf Data Cell ki alag request packet ko RECEIVED karti hai.
    [HttpPost("sealed-receipt")]
    public Task<IActionResult> Receive(int approvalId, ReceiptRequest request, CancellationToken cancellation)
        => ReadAsync("dbo.usp_MarkPaperReceived",["receipt"],cancellation,new SqlParameter("@ApprovalReviewId",approvalId),
            new SqlParameter("@DeliveredBy",request.DeliveredByUserId),
            new SqlParameter("@Remarks",SqlDbType.NVarChar,-1){Value=(object?)request.Remarks ?? DBNull.Value});
}

public class ReceiptRequest
{
    [Range(1,int.MaxValue)] public int DeliveredByUserId { get; set; }
    public string? Remarks { get; set; }
}

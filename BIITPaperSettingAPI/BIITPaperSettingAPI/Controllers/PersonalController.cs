using System.ComponentModel.DataAnnotations;
using System.Data;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BIITPaperSettingAPI.Controllers;

[Route("api/me")]
public class PersonalController(SetupProcedures procedures) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: PERSONAL PROFILE AND NOTIFICATIONS
    // =====================================================
    // Owner ki ID signed login se milti hai. Profile mein administrative fields read-only hain.
    [HttpGet("profile")]
    public Task<IActionResult> Profile(CancellationToken cancellation)
        => ReadAsync("dbo.usp_GetProfile",["profile","roles"],cancellation);

    [HttpPut("profile")]
    public Task<IActionResult> Update(ProfileUpdate request,CancellationToken cancellation)
        => WriteAsync("dbo.usp_UpdateProfile",cancellation,new SqlParameter("@FullName",request.FullName.Trim()),
            new SqlParameter("@Phone",SqlDbType.NVarChar,30){Value=(object?)request.Phone ?? DBNull.Value},
            new SqlParameter("@PhotoFileId",SqlDbType.Int){Value=(object?)request.PhotoFileId ?? DBNull.Value});

    [HttpGet("notifications")]
    public Task<IActionResult> Notifications(CancellationToken cancellation,bool unreadOnly=false)
        => ReadAsync("dbo.usp_GetNotifications",["notifications"],cancellation,new SqlParameter("@UnreadOnly",unreadOnly));

    [HttpPut("notifications/{id:int:min(1)}/read")]
    public Task<IActionResult> MarkRead(int id,CancellationToken cancellation)
        => WriteAsync("dbo.usp_MarkNotificationRead",cancellation,new SqlParameter("@NotificationId",id));

    // =====================================================
    // STEP 2: KEEP PERSONAL TASKS AND THEIR TRASH PRIVATE
    // =====================================================
    [HttpGet("wishlist")]
    public Task<IActionResult> Wishlist(CancellationToken cancellation,bool trash=false)
        => ReadAsync("dbo.usp_GetWishlist",["items"],cancellation,new SqlParameter("@Trash",trash));

    [HttpPost("wishlist")]
    public Task<IActionResult> Create(WishlistCreate request,CancellationToken cancellation)
        => Save(null,request.PaperId,request,cancellation);

    [HttpPut("wishlist/{id:int:min(1)}")]
    public Task<IActionResult> Edit(int id,WishlistUpdate request,CancellationToken cancellation)
        => Save(id,null,request,cancellation);

    private Task<IActionResult> Save(int? id,int? paperId,WishlistUpdate request,CancellationToken cancellation)
        => WriteAsync("dbo.usp_SaveWishlist",cancellation,new SqlParameter("@Title",request.Title.Trim()),
            new SqlParameter("@Note",SqlDbType.NVarChar,-1){Value=(object?)request.Note ?? DBNull.Value},
            new SqlParameter("@DueAt",SqlDbType.DateTime2){Value=(object?)request.DueAt?.UtcDateTime ?? DBNull.Value},
            new SqlParameter("@WishlistId",SqlDbType.Int){Value=(object?)id ?? DBNull.Value},
            new SqlParameter("@PaperId",SqlDbType.Int){Value=(object?)paperId ?? DBNull.Value});

    [HttpPut("wishlist/{id:int:min(1)}/state/{state}")]
    public Task<IActionResult> SetState(int id,[RegularExpression("^(DONE|UNDONE|TRASH|RESTORE)$")] string state,CancellationToken cancellation)
        => WriteAsync("dbo.usp_SetWishlistState",cancellation,new SqlParameter("@WishlistId",id),new SqlParameter("@Action",state));
}

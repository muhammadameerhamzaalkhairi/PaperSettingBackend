using System.ComponentModel.DataAnnotations;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;

namespace BIITPaperSettingAPI.Controllers;

[Route("api/staff")]
public class StaffController(SetupProcedures procedures, PasswordHasher<string> hasher) : SetupController(procedures)
{
    // =====================================================
    // STEP 1: STAFF SETUP
    // =====================================================
    // SQL procedure staff ka role check karke apni transaction chalati hai.

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_CreateUser", cancellation,
            new SqlParameter("@StaffNumber", request.StaffNumber),
            new SqlParameter("@FullName", request.FullName),
            new SqlParameter("@Email", request.Email),
            new SqlParameter("@PasswordHash", hasher.HashPassword(request.Email, request.Password)),
            new SqlParameter("@DepartmentId", (object?)request.DepartmentId ?? DBNull.Value),
            new SqlParameter("@Designation", (object?)request.Designation ?? DBNull.Value));
    }

    [HttpPut("accounts/{id:int}")]
    public async Task<IActionResult> UpdateStaffDetails([Range(1, int.MaxValue)] int id, UpdateStaffDetailsRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_UpdateStaffDetails", cancellation,
            new SqlParameter("@UserId", id),
            new SqlParameter("@FullName", request.FullName),
            new SqlParameter("@Email", request.Email),
            new SqlParameter("@Designation", (object?)request.Designation ?? DBNull.Value),
            new SqlParameter("@Phone", (object?)request.Phone ?? DBNull.Value));
    }

    [HttpPut("accounts/{id:int}/active")]
    public async Task<IActionResult> SetStaffActive([Range(1, int.MaxValue)] int id, SetStaffActiveRequest request, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_SetStaffActive", cancellation,
            new SqlParameter("@UserId", id),
            new SqlParameter("@IsActive", request.IsActive));
    }

    [HttpPost("roles")]
    public async Task<IActionResult> GrantUserRole(GrantUserRoleRequest request, CancellationToken cancellation)
    {
        if (request.RoleCode == "HOD" && request.DepartmentId == null)
            return BadRequest("Invalid GrantUserRole request.");
        return await WriteAsync("dbo.usp_GrantUserRole", cancellation,
            new SqlParameter("@UserId", request.UserId),
            new SqlParameter("@RoleCode", request.RoleCode),
            new SqlParameter("@DepartmentId", (object?)request.DepartmentId ?? DBNull.Value));
    }

    [HttpPost("roles/{id:int}/end")]
    public async Task<IActionResult> EndUserRole([Range(1, int.MaxValue)] int id, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_EndUserRole", cancellation,
            new SqlParameter("@UserRoleId", id));
    }

    [HttpPost("delegations")]
    public async Task<IActionResult> GrantDirectorDelegation(GrantDirectorDelegationRequest request, CancellationToken cancellation)
    {
        if (request.EndsAt <= request.StartsAt)
            return BadRequest("Invalid GrantDirectorDelegation request.");
        return await WriteAsync("dbo.usp_GrantDirectorDelegation", cancellation,
            new SqlParameter("@DelegateId", request.DelegateId),
            new SqlParameter("@StartsAt", request.StartsAt!.Value.UtcDateTime),
            new SqlParameter("@EndsAt", request.EndsAt!.Value.UtcDateTime),
            new SqlParameter("@Reason", (object?)request.Reason ?? DBNull.Value));
    }

    [HttpPost("delegations/{id:int}/revoke")]
    public async Task<IActionResult> RevokeDirectorDelegation([Range(1, int.MaxValue)] int id, CancellationToken cancellation)
    {
        return await WriteAsync("dbo.usp_RevokeDirectorDelegation", cancellation,
            new SqlParameter("@DelegationId", id));
    }

    [HttpGet("delegations")]
    public Task<IActionResult> GetDelegations(CancellationToken cancellation)
    {
        return ReadAsync("dbo.usp_GetDelegations", ["delegations"], cancellation);
    }
}

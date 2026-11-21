using System.Data;
using BIITPaperSettingAPI.DTOs;
using BIITPaperSettingAPI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BIITPaperSettingAPI.Services;

public class StaffAccessService(BiitDbContext db, IConfiguration config, PasswordHasher<string> hasher)
{
    // =====================================================
    // STEP 1: VERIFY STAFF PASSWORD
    // =====================================================
    // Hash sirf server par check hota hai. Demo placeholder login nahi kar sakta.
    public async Task<int?> VerifyPasswordAsync(LoginRequest request, CancellationToken cancellation)
    {
        var connectionString = config.GetConnectionString("Authentication") ?? db.Database.GetConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand("dbo.usp_GetLoginRecord", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = request.Email.Trim();
        await connection.OpenAsync(cancellation);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation) || !reader.GetBoolean(2))
            return null;

        try
        {
            var result = hasher.VerifyHashedPassword(request.Email, reader.GetString(1), request.Password);
            if (result == PasswordVerificationResult.Failed)
                return null;
        }
        catch (FormatException)
        {
            return null;
        }
        return reader.GetInt32(0);
    }

    // =====================================================
    // STEP 2: READ ACTIVE EXAM FOR VERIFIED STAFF
    // =====================================================
    // Procedure har request par active staff aur role dobara check karti hai.
    public async Task<CurrentExamResponse?> GetCurrentAsync(int actorId, CancellationToken cancellation)
    {
        try
        {
            var rows = await db.Database.SqlQuery<CurrentExamRow>(
                $"EXEC dbo.usp_GetCurrentExam @ActorId={actorId}").ToListAsync(cancellation);
            if (rows.Count == 0)
                return new CurrentExamResponse();

            var row = rows[0];
            return new CurrentExamResponse
            {
                SettingsId = row.settings_id, ActiveExamCycleId = row.active_exam_cycle_id,
                ActiveDatesheetId = row.active_datesheet_id, SessionId = row.session_id,
                SessionName = row.session_name, ExamType = row.exam_type,
                RevisionNumber = row.revision_number, DatesheetStatus = row.datesheet_status,
                UpdatedAt = row.updated_at
            };
        }
        catch (SqlException error) when (error.Number == 51001)
        {
            return null;
        }
    }
}

// SQL ke result columns bilkul procedure ke naam se map hote hain.
internal class CurrentExamRow
{
    public int settings_id { get; set; }
    public int? active_exam_cycle_id { get; set; }
    public int? active_datesheet_id { get; set; }
    public int? session_id { get; set; }
    public string? session_name { get; set; }
    public string? exam_type { get; set; }
    public int? revision_number { get; set; }
    public string? datesheet_status { get; set; }
    public DateTime? updated_at { get; set; }
}

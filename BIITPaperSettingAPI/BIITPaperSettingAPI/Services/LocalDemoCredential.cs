using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BIITPaperSettingAPI.Services;

public static class LocalDemoCredential
{
    // =====================================================
    // STEP 1: SET A LOCAL DEMO PASSWORD ONCE
    // =====================================================
    // Sirf LocalDB par Windows operator demo placeholder replace kar sakta hai.
    public static async Task SetAsync(string connectionString, string email)
    {
        var settings = new SqlConnectionStringBuilder(connectionString);
        if (!settings.DataSource.StartsWith(@"(localdb)\", StringComparison.OrdinalIgnoreCase)
            || !settings.IntegratedSecurity || !email.EndsWith("@example.invalid", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("An integrated LocalDB connection and a demo email are required.");
        if (!Console.IsInputRedirected)
            throw new InvalidOperationException("Pipe a masked password prompt to this command.");
        var password = Console.ReadLine();
        if (password == null || password.Length < 12 || password.Length > 1024)
            throw new InvalidOperationException("Use a password of 12 to 1024 characters.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        const string sql = """
            UPDATE dbo.APP_USER SET password_hash=@hash
            WHERE is_active=1 AND email=@email AND staff_number LIKE 'DEMO-%'
              AND password_hash=N'DEMO_ONLY_NOT_A_VALID_ASPNET_PASSWORD_HASH'
            """;
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@email", SqlDbType.NVarChar, 256).Value = email;
        command.Parameters.Add("@hash", SqlDbType.NVarChar, 256).Value =
            new PasswordHasher<string>().HashPassword(email, password);
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException("No eligible demo placeholder found.");
    }
}

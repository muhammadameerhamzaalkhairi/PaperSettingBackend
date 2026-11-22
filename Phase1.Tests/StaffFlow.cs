using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using BIITPaperSettingAPI.DTOs;

namespace Phase1.Tests;

[TestClass]
public class StaffFlow
{
    [TestMethod]
    public async Task StaffLoginAndLiveExam()
    {
        string admin = Environment.GetEnvironmentVariable("Phase1Admin") ?? throw new InvalidOperationException("Use Run-Local.ps1");
        var settings = new SqlConnectionStringBuilder(admin);
        Assert.IsTrue(settings.DataSource.StartsWith(@"(localdb)\BIITTest", StringComparison.OrdinalIgnoreCase));
        await using var db = new SqlConnection(admin);
        await db.OpenAsync();
        await using var restricted = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));
        await restricted.OpenAsync();
        using var permissions = new SqlCommand("SELECT IS_ROLEMEMBER('db_owner') + HAS_PERMS_BY_NAME('dbo.APP_USER','OBJECT','SELECT')", restricted);
        Assert.AreEqual(0, Convert.ToInt32(await permissions.ExecuteScalarAsync()));

        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(Environment.GetEnvironmentVariable("Phase1Dll")!);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add("http://127.0.0.1:0");
        start.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
        var privateFolder = Directory.CreateTempSubdirectory("BIIT-Diagrams-");
        start.Environment["Files__PrivateRoot"] = privateFolder.FullName;
        using var api = Process.Start(start)!;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            string? line;
            do {
                line = await api.StandardOutput.ReadLineAsync(timeout.Token);
                Assert.IsNotNull(line);
            } while (!line.Contains("Now listening on: "));
            using var http = new HttpClient { BaseAddress = new Uri(line.Split("Now listening on: ")[1].Trim()) };
            var drainLogs = api.StandardOutput.ReadToEndAsync();
            string password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var form = new { Email = "director@example.invalid", Password = password };
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/api/auth/login", form)).StatusCode);
            var setup = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true };
            setup.ArgumentList.Add(Environment.GetEnvironmentVariable("Phase1Dll")!);
            setup.ArgumentList.Add("--provision-local-demo");
            setup.ArgumentList.Add(form.Email);
            setup.Environment["ConnectionStrings__DefaultConnection"] = admin;
            using var provision = Process.Start(setup)!;
            await provision.StandardInput.WriteLineAsync(password);
            provision.StandardInput.Close();
            await provision.WaitForExitAsync();
            Assert.AreEqual(0, provision.ExitCode);
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/api/auth/login", new {form.Email, Password="incorrect"})).StatusCode);
            var login = await http.PostAsJsonAsync("/api/auth/login", form);
            login.EnsureSuccessStatusCode();
            var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
            const string route = "/api/exams/current";
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await http.GetAsync(route)).StatusCode);
            var key = Convert.FromBase64String(Environment.GetEnvironmentVariable("Jwt__SigningKey")!);
            foreach (var invalid in new[] { "malformed", Sign(key, -1), Sign(RandomNumberGenerator.GetBytes(32), 5), Sign(key, 5, "wrong"), Sign(key, 5, audience:"wrong") })
            {
                http.DefaultRequestHeaders.Authorization = new("Bearer", invalid);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await http.GetAsync(route)).StatusCode);
            }
            http.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var exam = await http.GetFromJsonAsync<CurrentExamResponse>(route);
            Assert.AreEqual("Final", exam!.ExamType);
            Assert.AreEqual("PUBLISHED", exam.DatesheetStatus);
            await AcademicSetupFlow.VerifyAsync(http, db, restricted, token);
            var changed = await http.GetFromJsonAsync<CurrentExamResponse>(route);
            Assert.AreNotEqual(exam.ActiveExamCycleId, changed!.ActiveExamCycleId);
            Assert.AreEqual("Coverage Test", changed.SessionName);
            await Execute(db, "UPDATE dbo.SYSTEM_SETTINGS SET active_exam_cycle_id=NULL,active_datesheet_id=NULL");
            var empty = await http.GetFromJsonAsync<CurrentExamResponse>(route);
            Assert.IsNull(empty!.ActiveExamCycleId);
            Assert.IsNull(empty.DatesheetStatus);
            await Execute(db, "UPDATE dbo.APP_USER SET is_active=0 WHERE email='director@example.invalid'");
            Assert.AreEqual(HttpStatusCode.Forbidden, (await http.GetAsync(route)).StatusCode);
            await Execute(db, "UPDATE dbo.APP_USER SET is_active=1 WHERE email='director@example.invalid'; UPDATE dbo.USER_ROLE SET valid_until=DATEADD(minute,-1,SYSUTCDATETIME()) WHERE user_id=(SELECT user_id FROM dbo.APP_USER WHERE email='director@example.invalid')");
            Assert.AreEqual(HttpStatusCode.Forbidden, (await http.GetAsync(route)).StatusCode);
        }
        finally
        {
            if (!api.HasExited) api.Kill(true);
            await api.WaitForExitAsync();
            privateFolder.Delete(true);
        }
    }

    private static string Sign(byte[] key, int minutes, string issuer="BIIT.PaperSetting", string audience="BIIT.Staff")
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(issuer, audience, [new Claim("sub","1")], expires: DateTime.UtcNow.AddMinutes(minutes), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task Execute(SqlConnection db, string sql)
    {
        using var command = new SqlCommand(sql, db);
        await command.ExecuteNonQueryAsync();
    }
}

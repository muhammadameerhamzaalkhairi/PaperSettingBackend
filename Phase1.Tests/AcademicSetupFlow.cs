using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using BIITPaperSettingAPI.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace Phase1.Tests;

internal static class AcademicSetupFlow
{
    public static async Task VerifyAsync(HttpClient http, SqlConnection db, SqlConnection restricted, string directorToken)
    {
        string password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        foreach (string email in new[] { "hod@example.invalid", "datacell@example.invalid", "amir@example.invalid", "saeed@example.invalid" })
        {
            using var seed = new SqlCommand("UPDATE dbo.APP_USER SET password_hash=@hash WHERE email=@email", db);
            seed.Parameters.AddWithValue("@hash", new PasswordHasher<string>().HashPassword(email, password));
            seed.Parameters.AddWithValue("@email", email);
            Assert.AreEqual(1, await seed.ExecuteNonQueryAsync());
        }

        var hod = await Login(http, "hod@example.invalid", password);
        var dataCell = await Login(http, "datacell@example.invalid", password);
        http.DefaultRequestHeaders.Authorization = new("Bearer", dataCell);
        int department = await Create(http, "academic/departments", new {code="BIO", name="Biology"});
        int program = await Create(http, "academic/programs", new {departmentId=department, code="BSBIO", name="Biology"});
        int session = await Create(http, "academic/sessions", new {name="Setup test", startsOn="2028-01-01", endsOn="2028-06-30"});
        int section = await Create(http, "academic/sections", new {programId=program, sessionId=session, name="A", semesterNumber=1});
        int course = await Create(http, "academic/courses", new {departmentId=department, code="BIO101", title="Biology"});
        int cycle = await Create(http, "academic/exam-cycles", new {sessionId=session, examType="Final"});
        var account = new {staffNumber="TEST-1", fullName="Setup fixture", email="setup@example.invalid", password, departmentId=department};
        int staff = await Create(http, "staff/accounts", account);
        var grant = new { userId=staff, roleCode="HOD", departmentId=department };
        Assert.AreEqual(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/staff/roles", grant)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/auth/login", new {account.email,password})).StatusCode);
        http.DefaultRequestHeaders.Authorization = new("Bearer", hod);
        var draft = new {courseId=course, versionNumber=1};
        Assert.AreEqual(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/curriculum/outlines", draft)).StatusCode);
        http.DefaultRequestHeaders.Authorization = new("Bearer", directorToken);
        await Create(http, "staff/roles", grant);
        int teacherRole = await Create(http, "staff/roles", new {userId=staff, roleCode="TEACHER", departmentId=department});
        // SQL grants start at datetime2(0) precision.
        await Task.Delay(1100);
        var staffToken = await Login(http, account.email, password);
        http.DefaultRequestHeaders.Authorization = new("Bearer", staffToken);
        int outline = await Create(http, "curriculum/outlines", draft);
        int topic = await Create(http, "curriculum/topics", new {outlineId=outline,title="Cells"});
        int clo = await Create(http, "curriculum/clos", new {outlineId=outline,code="C1",statement="Describe cells"});
        (await http.PutAsJsonAsync($"/api/curriculum/topics/{topic}", new {title="Cells",week=1})).EnsureSuccessStatusCode();
        (await http.PutAsJsonAsync($"/api/curriculum/clos/{clo}", new {code="C1",statement="Explain cells"})).EnsureSuccessStatusCode();
        (await http.PostAsync($"/api/curriculum/outlines/{outline}/publish", null)).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Conflict, (await http.PutAsJsonAsync($"/api/curriculum/topics/{topic}",new {title="Replaced"})).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, (await http.PutAsJsonAsync($"/api/curriculum/clos/{clo}",new {code="C2",statement="Replacement"})).StatusCode);
        int revision = await Create(http, "curriculum/outlines", new {courseId=course,versionNumber=2});
        Assert.AreNotEqual(outline, revision);
        using var history = new SqlCommand("SELECT topic_title FROM dbo.TOPIC WHERE topic_id=@topic", db);
        history.Parameters.AddWithValue("@topic", topic);
        Assert.AreEqual("Cells", await history.ExecuteScalarAsync());

        http.DefaultRequestHeaders.Authorization = new("Bearer", dataCell);
        int offering = await Create(http, "academic/offerings", new {courseId=course,outlineId=outline,sessionId=session,departmentId=department});
        int link = await Create(http, "academic/offering-sections", new {offeringId=offering,sectionId=section});
        int assignment = await Create(http, "academic/teaching-assignments",new {offeringSectionId=link,teacherId=staff});
        (await http.PutAsJsonAsync($"/api/staff/accounts/{staff}",new {fullName="Renamed fixture",account.email})).EnsureSuccessStatusCode();
        var lookups = await http.GetFromJsonAsync<JsonElement>("/api/academic/lookups");
        Assert.AreEqual(8, lookups.EnumerateObject().Count());
        http.DefaultRequestHeaders.Authorization = new("Bearer", staffToken);
        int exam = await Create(http, "curriculum/exams",new {offeringId=offering,examCycleId=cycle,marks=60,duration=90});
        (await http.PutAsJsonAsync($"/api/curriculum/exams/{exam}",new {marks=65,duration=100})).EnsureSuccessStatusCode();
        (await http.PutAsJsonAsync($"/api/curriculum/exams/{exam}/clos",new {cloIds=new[]{clo}})).EnsureSuccessStatusCode();
        http.DefaultRequestHeaders.Authorization = new("Bearer", directorToken);
        int delegation = await Create(http, "staff/delegations",new {delegateId=staff,startsAt=DateTimeOffset.UtcNow.AddHours(-2),endsAt=DateTimeOffset.UtcNow.AddHours(1)});
        var active = await http.GetFromJsonAsync<JsonElement>("/api/staff/delegations");
        Assert.AreEqual("ACTIVE", active.GetProperty("delegations")[0].GetProperty("status").GetString());
        using var expire = new SqlCommand("""
            UPDATE dbo.DIRECTOR_DELEGATION SET ends_at=DATEADD(hour,-1,SYSUTCDATETIME()) WHERE delegation_id=@id;
            UPDATE dbo.PAPER SET workflow_status='HOD_REVIEWED' WHERE workflow_status='APPROVED';
            SELECT MIN(submission_id) FROM dbo.PAPER_SUBMISSION;
            """,db);
        expire.Parameters.AddWithValue("@id",delegation);
        int submission = Convert.ToInt32(await expire.ExecuteScalarAsync());
        http.DefaultRequestHeaders.Authorization = new("Bearer", staffToken);
        var list = await http.GetFromJsonAsync<JsonElement>("/api/staff/delegations");
        Assert.AreEqual("EXPIRED",list.GetProperty("delegations")[0].GetProperty("status").GetString());
        using var approve = new SqlCommand("EXEC dbo.usp_ReviewPaper @ActorId,@SubmissionId,'DIRECTOR','APPROVED',NULL,@DelegationId", restricted);
        approve.Parameters.AddWithValue("@ActorId",staff);
        approve.Parameters.AddWithValue("@SubmissionId",submission);
        approve.Parameters.AddWithValue("@DelegationId",delegation);
        try
        {
            await approve.ExecuteNonQueryAsync();
            Assert.Fail("Expired delegation approved a paper.");
        }
        catch (SqlException error)
        {
            Assert.AreEqual(51040,error.Number);
            StringAssert.Contains(error.Message,"Director delegation expired or invalid");
        }
        http.DefaultRequestHeaders.Authorization = new("Bearer",directorToken);
        (await http.PostAsync($"/api/staff/delegations/{delegation}/revoke", null)).EnsureSuccessStatusCode();
        (await http.PostAsync($"/api/staff/roles/{teacherRole}/end", null)).EnsureSuccessStatusCode();
        (await http.PutAsJsonAsync($"/api/staff/accounts/{staff}/active",new {isActive=false})).EnsureSuccessStatusCode();
        http.DefaultRequestHeaders.Authorization = new("Bearer", dataCell);
        // Assignment ka end starts_at se baad hona chahiye (SQL second precision).
        await Task.Delay(1100);
        (await http.PostAsync($"/api/academic/teaching-assignments/{assignment}/end", null)).EnsureSuccessStatusCode();
        await DatesheetFlow.VerifyAsync(http, db, dataCell, directorToken);
        var firstTeacher = await Login(http, "amir@example.invalid", password);
        var secondTeacher = await Login(http, "saeed@example.invalid", password);
        await CoverageFlow.VerifyAsync(http, db, directorToken, hod, firstTeacher, secondTeacher);
        await CheckingFlow.VerifyAsync(http, db, restricted, hod, firstTeacher);
        await ReviewFlow.VerifyAsync(http, db, directorToken, hod, firstTeacher, secondTeacher);
        http.DefaultRequestHeaders.Authorization = new("Bearer",directorToken);
    }

    private static async Task<int> Create(HttpClient http,string route,object request)
    {
        using var result = await http.PostAsJsonAsync("/api/"+route,request);
        Assert.IsTrue(result.IsSuccessStatusCode, $"{route} returned {result.StatusCode}");
        var created = await result.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetInt32();
    }

    private static async Task<string> Login(HttpClient http,string email,string password)
    {
        using var result = await http.PostAsJsonAsync("/api/auth/login",new {email,password});
        result.EnsureSuccessStatusCode();
        return (await result.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
    }
}

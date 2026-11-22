using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Phase1.Tests;

internal static class DashboardFlow
{
    // Active HOD ko apne department se bahar paper list aur detail dono par rokna hai.
    public static async Task OutsideDepartment(HttpClient http, SqlConnection db, string token)
    {
        using var cmd=new SqlCommand("""
            SELECT TOP(1) e.exam_cycle_id,p.paper_id,v.paper_version_id FROM dbo.vw_CourseExam e
            JOIN dbo.PAPER p ON p.course_exam_id=e.course_exam_id JOIN dbo.PAPER_VERSION v ON v.paper_id=p.paper_id
            WHERE e.course_code='CS-301' ORDER BY p.paper_id DESC
            """,db);
        using var read=await cmd.ExecuteReaderAsync();
        Assert.IsTrue(await read.ReadAsync());
        int cycle=read.GetInt32(0),paper=read.GetInt32(1),version=read.GetInt32(2);
        http.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var list=await http.GetFromJsonAsync<JsonElement>($"/api/papers?examCycleId={cycle}&search=CS-301");
        Assert.AreEqual(0,list.GetProperty("papers").GetArrayLength());
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.GetAsync($"/api/papers/{paper}/versions")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PostAsJsonAsync($"/api/paper-versions/{version}/comments",new {text="Denied"})).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PostAsJsonAsync("/api/me/wishlist",new {title="Denied",paperId=paper})).StatusCode);
    }

    public static async Task VerifyAsync(HttpClient http,string director,string dataCell,string hod,string teacher,string acting)
    {
        void As(string token)=>http.DefaultRequestHeaders.Authorization=new("Bearer",token);
        http.DefaultRequestHeaders.Authorization=null;
        Assert.AreEqual(HttpStatusCode.Unauthorized,(await http.GetAsync("/api/dashboard")).StatusCode);
        int paper=0,version=0;
        foreach(string token in new[]{director,dataCell,hod,teacher,acting})
        {
            As(token);
            var dashboard=await http.GetFromJsonAsync<JsonElement>("/api/dashboard");
            Assert.AreEqual("RECEIVED",dashboard.GetProperty("statuses")[0].GetProperty("workflow_status").GetString());
            var courses=await http.GetFromJsonAsync<JsonElement>("/api/courses");
            Assert.AreEqual(1,courses.GetProperty("courses").GetArrayLength());
            var list=await http.GetFromJsonAsync<JsonElement>("/api/papers?status=RECEIVED&search=CS-301&pageSize=1");
            var row=list.GetProperty("papers")[0];
            paper=row.GetProperty("paper_id").GetInt32();
            version=row.GetProperty("paper_version_id").GetInt32();
            var versions=await http.GetFromJsonAsync<JsonElement>($"/api/papers/{paper}/versions");
            Assert.AreEqual(version,versions.GetProperty("versions")[0].GetProperty("paper_version_id").GetInt32());
            (await http.GetAsync($"/api/paper-versions/{version}")).EnsureSuccessStatusCode();
        }
        string root=$"/api/paper-versions/{version}";
        As(teacher);
        Assert.AreEqual(HttpStatusCode.BadRequest,(await http.GetAsync("/api/papers?pageSize=101")).StatusCode);
        var context=await http.GetFromJsonAsync<JsonElement>("/api/exams/current");
        (await http.GetAsync("/api/dashboard?examCycleId=1")).EnsureSuccessStatusCode();
        Assert.AreEqual(context.GetRawText(),(await http.GetFromJsonAsync<JsonElement>("/api/exams/current")).GetRawText());
        var profile=await http.GetFromJsonAsync<JsonElement>("/api/me/profile");
        var user=profile.GetProperty("profile")[0];
        Assert.IsFalse(user.TryGetProperty("password_hash",out _));
        (await http.PutAsJsonAsync("/api/me/profile",new {fullName=user.GetProperty("full_name").GetString(),phone="123"})).EnsureSuccessStatusCode();
        var updated=await http.GetFromJsonAsync<JsonElement>("/api/me/profile");
        Assert.AreEqual("123",updated.GetProperty("profile")[0].GetProperty("phone_number").GetString());
        var note=await http.PostAsJsonAsync(root+"/comments",new {text="Check the last part"});
        note.EnsureSuccessStatusCode();
        int parent=(await note.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.AreEqual(HttpStatusCode.BadRequest,(await http.PostAsJsonAsync(root+"/comments",
            new {text="Unknown parent",parentCommentId=int.MaxValue})).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest,(await http.PostAsJsonAsync(root+"/comments",
            new {text="Unknown item",paperItemId=int.MaxValue})).StatusCode);
        As(hod);
        (await http.PostAsJsonAsync(root+"/comments",new {text="Checked",parentCommentId=parent})).EnsureSuccessStatusCode();
        var detail=await http.GetFromJsonAsync<JsonElement>(root);
        Assert.IsTrue(detail.GetProperty("comments").EnumerateArray().Any(row=>row.GetProperty("parent_comment_id").ValueKind==JsonValueKind.Number &&
            row.GetProperty("parent_comment_id").GetInt32()==parent));
        As(teacher);
        var create=await http.PostAsJsonAsync("/api/me/wishlist",new {title="Check archive",paperId=paper});
        create.EnsureSuccessStatusCode();
        int wish=(await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        string task=$"/api/me/wishlist/{wish}";
        As(director);
        Assert.AreEqual(HttpStatusCode.NotFound,(await http.PutAsync(task+"/state/TRASH",null)).StatusCode);
        As(teacher);
        (await http.PutAsJsonAsync(task,new {title="Review archive"})).EnsureSuccessStatusCode();
        foreach(string action in new[]{"DONE","UNDONE","TRASH"})
            (await http.PutAsync(task+"/state/"+action,null)).EnsureSuccessStatusCode();
        var trash=await http.GetFromJsonAsync<JsonElement>("/api/me/wishlist?trash=true");
        Assert.AreEqual(wish,trash.GetProperty("items")[0].GetProperty("wishlist_id").GetInt32());
        (await http.PutAsync(task+"/state/RESTORE",null)).EnsureSuccessStatusCode();
        var alerts=await http.GetFromJsonAsync<JsonElement>("/api/me/notifications?unreadOnly=true");
        int alert=alerts.GetProperty("notifications")[0].GetProperty("notification_id").GetInt32();
        As(director);
        Assert.AreEqual(HttpStatusCode.NotFound,(await http.PutAsync($"/api/me/notifications/{alert}/read",null)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PostAsync($"/api/papers/{paper}/archive",null)).StatusCode);
        As(teacher);
        (await http.PutAsync($"/api/me/notifications/{alert}/read",null)).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.GetAsync("/api/imports/history")).StatusCode);
        As(dataCell);
        (await http.GetAsync("/api/imports/history")).EnsureSuccessStatusCode();
        (await http.PostAsync($"/api/papers/{paper}/archive",null)).EnsureSuccessStatusCode();
        var archived=await http.GetFromJsonAsync<JsonElement>("/api/papers?status=ARCHIVED");
        Assert.AreEqual(paper,archived.GetProperty("papers")[0].GetProperty("paper_id").GetInt32());
        Assert.AreEqual(HttpStatusCode.Conflict,(await http.PostAsync($"/api/papers/{paper}/archive",null)).StatusCode);
        (await http.GetAsync($"/api/papers/{paper}/versions")).EnsureSuccessStatusCode();
        As(director);
    }
}

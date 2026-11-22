using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Phase1.Tests;

internal static class ReviewFlow
{
    public static async Task VerifyAsync(HttpClient http, SqlConnection db, string director, string hod, string setter, string acting)
    {
        void As(string token) => http.DefaultRequestHeaders.Authorization = new("Bearer", token);
        async Task<JsonElement> Workspace(int id) => await http.GetFromJsonAsync<JsonElement>($"/api/paper-versions/{id}");
        async Task<int> Lookup(string sql, params SqlParameter[] parameters)
        {
            using var command = new SqlCommand(sql,db);
            command.Parameters.AddRange(parameters);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        async Task Align(int id)
        {
            As(hod);
            var workspace = await Workspace(id);
            foreach (var mapping in workspace.GetProperty("cloMappings").EnumerateArray())
            {
                if (mapping.GetProperty("check_status").GetString() == "ALIGNED") continue;
                int mappingId = mapping.GetProperty("mapping_id").GetInt32();
                (await http.PostAsJsonAsync($"/api/clo-mappings/{mappingId}/reviews",
                    new {status="ALIGNED",explanation="The revised question addresses the selected exam CLO."})).EnsureSuccessStatusCode();
            }
        }
        async Task<HttpResponseMessage> Submit(int id)
        {
            var workspace = await Workspace(id);
            return await http.PostAsJsonAsync($"/api/paper-versions/{id}/submit",
                new { rowVersion=workspace.GetProperty("header")[0].GetProperty("row_version").GetString() });
        }
        Task<HttpResponseMessage> Decide(int submission,int id,string decision,string? reason=null,int? delegation=null)
            => http.PostAsJsonAsync($"/api/submissions/{submission}/versions/{id}/decisions",
                new {stage="DIRECTOR",decision,reason,delegationId=delegation});

        // =====================================================
        // STEP 1: DIRECT REVIEW AND SETTER CORRECTION
        // =====================================================
        int original = await Lookup("""
            SELECT v.paper_version_id FROM dbo.vw_CourseExam e JOIN dbo.PAPER p ON p.course_exam_id=e.course_exam_id
            JOIN dbo.PAPER_VERSION v ON v.paper_id=p.paper_id WHERE e.session_name='Fall 2026' AND e.course_code='CS-301'
            """);
        As(setter);
        Assert.AreEqual(HttpStatusCode.Conflict,(await Submit(original)).StatusCode);
        await Align(original);
        As(acting);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Submit(original)).StatusCode);
        As(setter);
        int first = await Id(await Submit(original),"submission","submission_id");
        As(director);
        var actions = await http.GetFromJsonAsync<JsonElement>("/api/paper-review/actions");
        Assert.AreEqual("Request Changes",actions[1].GetProperty("label").GetString());
        Assert.AreEqual(HttpStatusCode.BadRequest,(await Decide(first,original,"CHANGES_REQUESTED","  ")).StatusCode);
        (await Decide(first,original,"CHANGES_REQUESTED","Clarify the instructions.")).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Conflict,(await Decide(first,original,"APPROVED")).StatusCode);

        As(setter);
        var old = await Workspace(original);
        int paper = await Lookup("SELECT paper_id FROM dbo.PAPER_VERSION WHERE paper_version_id=@id",new SqlParameter("@id",original));
        var started = await http.PostAsJsonAsync($"/api/papers/{paper}/versions",new {reason="Clarify instructions as requested"});
        int corrected = await Id(started,"version","paper_version_id");
        var correction = await Workspace(corrected);
        (await http.PutAsJsonAsync($"/api/paper-versions/{corrected}/instructions",new {
            rowVersion=correction.GetProperty("header")[0].GetProperty("row_version").GetString(),instructions="Attempt all four questions."
        })).EnsureSuccessStatusCode();
        int submission = await Id(await Submit(corrected),"submission","submission_id");
        Assert.AreNotEqual(first,submission);
        Assert.AreEqual(old.GetProperty("header")[0].GetProperty("instructions").GetString(),
            (await Workspace(original)).GetProperty("header")[0].GetProperty("instructions").GetString());

        // =====================================================
        // STEP 2: OPTIONAL HOD REVIEW BEFORE A DIRECTOR DECISION
        // =====================================================
        As(hod);
        (await http.PostAsJsonAsync($"/api/submissions/{submission}/versions/{corrected}/decisions",
            new {stage="HOD",decision="APPROVED"})).EnsureSuccessStatusCode();
        As(director);
        (await Decide(submission,corrected,"CHANGES_REQUESTED","Specify the time limit.")).EnsureSuccessStatusCode();
        As(setter);
        corrected = await Id(await http.PostAsJsonAsync($"/api/papers/{paper}/versions",
            new {reason="Add the time limit"}),"version","paper_version_id");
        correction = await Workspace(corrected);
        (await http.PutAsJsonAsync($"/api/paper-versions/{corrected}/instructions",new {
            rowVersion=correction.GetProperty("header")[0].GetProperty("row_version").GetString(),instructions="Time limit is two hours."
        })).EnsureSuccessStatusCode();
        submission = await Id(await Submit(corrected),"submission","submission_id");
        var beforeEdit = await Workspace(corrected);

        // =====================================================
        // STEP 3: EDIT A FROZEN VERSION, THEN RECHECK AUTHORITY
        // =====================================================
        async Task<HttpResponseMessage> Edit(int id,string stem,int? delegation=null,decimal marks=10)
        {
            var workspace = await Workspace(id);
            var item = workspace.GetProperty("questions").EnumerateArray().First(x=>x.GetProperty("placement").GetString()=="MAIN");
            int question = item.GetProperty("question_version_id").GetInt32();
            int itemId = item.GetProperty("paper_item_id").GetInt32();
            var part = workspace.GetProperty("parts").EnumerateArray().First(x=>x.GetProperty("question_version_id").GetInt32()==question);
            var mapping = workspace.GetProperty("cloMappings").EnumerateArray().First(x=>x.GetProperty("question_version_id").GetInt32()==question);
            return await http.PostAsJsonAsync($"/api/submissions/{submission}/versions/{id}/items/{itemId}/revisions",new {
                reason="Clarify the question wording",delegationId=delegation,prompt=stem,difficulty="EASY",placement="MAIN",
                rowVersion=workspace.GetProperty("header")[0].GetProperty("row_version").GetString(),
                parts=new[]{new {key=1,kind="ITEM",label="a",body="Explain",topicId=part.GetProperty("topic_id").GetInt32(),marks,difficulty="EASY"}},
                mappings=new[]{new {examCloId=mapping.GetProperty("exam_clo_id").GetInt32()}}
            });
        }
        As(acting);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Edit(corrected,"Denied")).StatusCode);
        As(director);
        Assert.AreEqual(HttpStatusCode.Conflict,(await Edit(corrected,"Invalid marks",marks:9)).StatusCode);
        int edited = await Id(await Edit(corrected,"Director wording"),"saved","paper_version_id");
        Assert.AreEqual(HttpStatusCode.Conflict,(await Decide(submission,corrected,"APPROVED")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict,(await Decide(submission,edited,"APPROVED")).StatusCode);
        int delegateId=await Lookup("SELECT user_id FROM dbo.APP_USER WHERE staff_number='DEMO-T2'");
        async Task<int> Grant()
        {
            As(director);
            var grant=await http.PostAsJsonAsync("/api/staff/delegations",new {
                delegateId,startsAt=DateTimeOffset.UtcNow.AddHours(-1),endsAt=DateTimeOffset.UtcNow.AddHours(1)
            });
            grant.EnsureSuccessStatusCode();
            return (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        }
        int authority=await Grant();
        As(acting);
        int final=await Id(await Edit(edited,"Acting Director wording",authority),"saved","paper_version_id");
        await Align(final);
        As(director);
        (await http.PostAsync($"/api/staff/delegations/{authority}/revoke",null)).EnsureSuccessStatusCode();
        As(acting);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Decide(submission,final,"APPROVED",delegation:authority)).StatusCode);
        int renewed=await Grant();
        As(acting);
        int approval=await Id(await Decide(submission,final,"APPROVED",delegation:renewed),"review","review_id");
        Assert.AreEqual(final,await Lookup("SELECT reviewed_version_id FROM dbo.REVIEW WHERE review_id=@id",new SqlParameter("@id",approval)));
        Assert.AreEqual(0,await Lookup("SELECT COUNT(*) FROM dbo.REVIEW WHERE review_stage='HOD' AND submission_id=@id",new SqlParameter("@id",submission)));
        Assert.AreEqual(1,await Lookup("SELECT COUNT(*) FROM dbo.AUDIT_EVENT WHERE action='DIRECTOR_EDIT' AND actor_user_id=@actor AND delegation_id=@grant",new SqlParameter("@actor",delegateId),new SqlParameter("@grant",authority)));
        var history=await http.GetFromJsonAsync<JsonElement>($"/api/papers/{paper}/versions");
        var latest = history.GetProperty("versions")[0];
        Assert.AreEqual(delegateId,latest.GetProperty("created_by_user_id").GetInt32());
        Assert.AreEqual(edited,latest.GetProperty("based_on_version_id").GetInt32());
        Assert.AreEqual("Clarify the question wording",latest.GetProperty("revision_reason").GetString());
        var preserved = await Workspace(corrected);
        Assert.AreEqual(beforeEdit.GetProperty("header")[0].GetProperty("row_version").GetString(),
            preserved.GetProperty("header")[0].GetProperty("row_version").GetString());
        Assert.AreEqual(beforeEdit.GetProperty("questions").GetRawText(),preserved.GetProperty("questions").GetRawText());
        Assert.AreEqual("Clarify the instructions.",history.GetProperty("reviews")[0].GetProperty("feedback").GetString());
        Assert.AreEqual("Director wording",(await Workspace(edited)).GetProperty("questions").EnumerateArray()
            .First(x=>x.GetProperty("placement").GetString()=="MAIN").GetProperty("prompt_text").GetString());
    }

    private static async Task<int> Id(HttpResponseMessage response,string section,string name)
    {
        Assert.IsTrue(response.IsSuccessStatusCode,$"{response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty(section)[0].GetProperty(name).GetInt32();
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Phase1.Tests;

internal static class CheckingFlow
{
    public static async Task VerifyAsync(HttpClient http, SqlConnection db, SqlConnection restricted, string hod, string teacher)
    {
        // Sirf disposable fixture mein doosra approved source aur aik matching EXTRA rakhein.
        using var fixture = new SqlCommand("""
            DECLARE @spring int, @fall int, @offering int, @session int, @actor int;
            SELECT @spring=v.paper_version_id,@offering=e.offering_id,@session=e.session_id,@actor=v.created_by_user_id
            FROM dbo.vw_CourseExam e JOIN dbo.PAPER p ON p.course_exam_id=e.course_exam_id
            JOIN dbo.PAPER_VERSION v ON v.paper_id=p.paper_id WHERE e.session_name='Spring 2026' AND e.course_code='CS-301';
            SELECT @fall=v.paper_version_id FROM dbo.vw_CourseExam e JOIN dbo.PAPER p ON p.course_exam_id=e.course_exam_id
            JOIN dbo.PAPER_VERSION v ON v.paper_id=p.paper_id WHERE e.session_name='Fall 2026' AND e.course_code='CS-301';
            INSERT dbo.EXAM_CYCLE(session_id,exam_type) VALUES(@session,'Mid');
            INSERT dbo.COURSE_EXAM(offering_id,exam_cycle_id,required_marks,duration_minutes,configured_by_user_id)
            VALUES(@offering,SCOPE_IDENTITY(),10,30,@actor);
            INSERT dbo.PAPER(course_exam_id,workflow_status) VALUES(SCOPE_IDENTITY(),'APPROVED');
            INSERT dbo.PAPER_VERSION(paper_id,revision_number,created_by_user_id,created_at,revision_reason,content_hash,
                required_marks_snapshot,duration_minutes_snapshot,frozen_at)
            VALUES(SCOPE_IDENTITY(),1,@actor,SYSUTCDATETIME(),'Synthetic source','fixture',10,30,SYSUTCDATETIME());
            DECLARE @source int=SCOPE_IDENTITY();
            INSERT dbo.PAPER_ITEM(paper_version_id,question_version_id,placement,sort_order)
            SELECT @source,question_version_id,'MAIN',1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@spring AND sort_order=1;
            INSERT dbo.PAPER_SUBMISSION(paper_version_id,submission_number,submitted_by_user_id,submitted_at)
            VALUES(@source,1,@actor,SYSUTCDATETIME());
            DECLARE @submission int=SCOPE_IDENTITY();
            INSERT dbo.REVIEW(submission_id,reviewed_version_id,reviewer_user_id,reviewer_user_role_id,review_stage,decision,reviewed_at)
            SELECT @submission,@source,reviewer_user_id,reviewer_user_role_id,'DIRECTOR','APPROVED',SYSUTCDATETIME()
            FROM dbo.REVIEW WHERE reviewed_version_id=@spring AND review_stage='DIRECTOR';
            INSERT dbo.PAPER_ITEM(paper_version_id,question_version_id,placement,sort_order)
            SELECT @fall,question_version_id,'EXTRA',1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@spring AND sort_order=4;
            SELECT @fall;
            """, db);
        int version = Convert.ToInt32(await fixture.ExecuteScalarAsync());
        string root = $"/api/paper-versions/{version}";
        http.DefaultRequestHeaders.Authorization = new("Bearer", teacher);
        var exact = await http.PostAsync(root + "/similarity/exact", null);
        exact.EnsureSuccessStatusCode();
        var run = (await exact.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("run")[0];
        Assert.AreEqual(4, run.GetProperty("total_main_questions").GetInt32());
        string reportRoute = "/api/similarity-runs/" + run.GetProperty("similarity_run_id").GetInt32();
        var report = await http.GetFromJsonAsync<JsonElement>(reportRoute);
        Assert.AreEqual("EXACT", report.GetProperty("run")[0].GetProperty("match_mode").GetString());
        var sources = report.GetProperty("sources");
        Assert.AreEqual(2, sources.GetArrayLength());
        Assert.AreEqual("Final", sources[0].GetProperty("exam_type").GetString());
        Assert.AreEqual(75m, sources[0].GetProperty("similarity_percent").GetDecimal());
        Assert.AreEqual(25m, sources[1].GetProperty("similarity_percent").GetDecimal());

        var workspace = await http.GetFromJsonAsync<JsonElement>(root);
        var mainQuestions = new HashSet<int>();
        foreach (var item in workspace.GetProperty("questions").EnumerateArray())
        {
            if (item.GetProperty("placement").GetString() == "MAIN")
                mainQuestions.Add(item.GetProperty("question_version_id").GetInt32());
        }
        var mainMappings = workspace.GetProperty("cloMappings").EnumerateArray()
            .Where(row => mainQuestions.Contains(row.GetProperty("question_version_id").GetInt32())).ToList();
        Assert.HasCount(4, mainMappings);
        var mapping = mainMappings[0];
        string review = "/api/clo-mappings/" + mapping.GetProperty("mapping_id").GetInt32() + "/reviews";
        var decision = new {status="ALIGNED", explanation="The question asks for the operations named in the exam CLO."};
        Assert.AreEqual(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync(review, decision)).StatusCode);
        http.DefaultRequestHeaders.Authorization = new("Bearer", hod);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync(review, new {status="ALIGNED", explanation=" "})).StatusCode);
        foreach (var mainMapping in mainMappings)
        {
            int mappingId = mainMapping.GetProperty("mapping_id").GetInt32();
            Assert.AreEqual(HttpStatusCode.NoContent, (await http.PostAsJsonAsync($"/api/clo-mappings/{mappingId}/reviews", decision)).StatusCode);
        }
        workspace = await http.GetFromJsonAsync<JsonElement>(root);
        var checkedRow = workspace.GetProperty("cloMappings").EnumerateArray()
            .Single(row => row.GetProperty("mapping_id").GetInt32() == mapping.GetProperty("mapping_id").GetInt32());
        Assert.AreEqual("ALIGNED", checkedRow.GetProperty("check_status").GetString());

        http.DefaultRequestHeaders.Authorization = new("Bearer", teacher);
        int question = mapping.GetProperty("question_version_id").GetInt32();
        var oldItem = workspace.GetProperty("questions").EnumerateArray().Single(row => row.GetProperty("question_version_id").GetInt32()==question);
        var part = workspace.GetProperty("parts").EnumerateArray().First(row => row.GetProperty("question_version_id").GetInt32()==question);
        var edit = new {
            rowVersion=workspace.GetProperty("header")[0].GetProperty("row_version").GetString(),
            prompt="Revised question", placement="MAIN", difficulty="EASY",
            parts=new[]{new {key=1,kind="ITEM",label="a",body="Explain",marks=10,topicId=part.GetProperty("topic_id").GetInt32(),difficulty="EASY"}},
            mappings=new[]{new {examCloId=mapping.GetProperty("exam_clo_id").GetInt32()}}
        };
        int itemId = oldItem.GetProperty("paper_item_id").GetInt32();
        (await http.PutAsJsonAsync($"{root}/items/{itemId}/question", edit)).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Conflict, (await http.GetAsync(reportRoute)).StatusCode);
        workspace = await http.GetFromJsonAsync<JsonElement>(root);
        var revisedItem = workspace.GetProperty("questions").EnumerateArray().Single(row => row.GetProperty("paper_item_id").GetInt32()==itemId);
        int newQuestion = revisedItem.GetProperty("question_version_id").GetInt32();
        Assert.AreNotEqual(question, newQuestion);
        var revisedMapping = workspace.GetProperty("cloMappings").EnumerateArray().Single(row => row.GetProperty("question_version_id").GetInt32()==newQuestion);
        Assert.AreEqual(JsonValueKind.Null, revisedMapping.GetProperty("check_status").ValueKind);
        using var actorLookup = new SqlCommand("SELECT user_id FROM dbo.APP_USER WHERE staff_number='DEMO-T1'", db);
        int actor = Convert.ToInt32(await actorLookup.ExecuteScalarAsync());
        using var submit = new SqlCommand("EXEC dbo.usp_SubmitPaper @actor,@version,@token", restricted);
        submit.Parameters.AddWithValue("@actor", actor);
        submit.Parameters.AddWithValue("@version", version);
        submit.Parameters.AddWithValue("@token", workspace.GetProperty("header")[0].GetProperty("row_version").GetBytesFromBase64());
        var denied = await Assert.ThrowsExactlyAsync<SqlException>(async () => await submit.ExecuteNonQueryAsync());
        Assert.AreEqual(51040, denied.Number);
        StringAssert.Contains(denied.Message, "ALIGNED");
        var availability = await http.GetFromJsonAsync<JsonElement>("/api/checking/semantic");
        Assert.AreEqual("DEFERRED", availability.GetProperty("status").GetString());
        foreach (string name in new[]{"usp_GetCheckingPayload","usp_SaveCLOCheckResults","usp_CompleteSemanticSimilarity","usp_FailSimilarityRun"})
        {
            using var permission = new SqlCommand("SELECT HAS_PERMS_BY_NAME(@name,'OBJECT','EXECUTE')", restricted);
            permission.Parameters.AddWithValue("@name", "dbo."+name);
            Assert.AreEqual(0, Convert.ToInt32(await permission.ExecuteScalarAsync()));
        }
    }
}

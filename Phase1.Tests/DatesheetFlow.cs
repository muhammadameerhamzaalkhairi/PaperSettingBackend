using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BIITPaperSettingAPI.DTOs;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;

namespace Phase1.Tests;

internal static class DatesheetFlow
{
    public static async Task VerifyAsync(HttpClient http, SqlConnection db, string dataCell, string director)
    {
        // Spring fixture mein do course exams rakhein taake missing row bhi test ho.
        using var seed = new SqlCommand("""
            DECLARE @cycle int=(SELECT MIN(exam_cycle_id) FROM dbo.EXAM_CYCLE);
            INSERT dbo.COURSE_OFFERING(course_id,outline_id,session_id,lead_department_id)
            SELECT o.course_id,o.outline_id,e.session_id,o.lead_department_id
            FROM dbo.COURSE_OFFERING o JOIN dbo.COURSE c ON c.course_id=o.course_id
            CROSS JOIN dbo.EXAM_CYCLE e WHERE c.course_code='BIO101' AND e.exam_cycle_id=@cycle;
            INSERT dbo.COURSE_EXAM(offering_id,exam_cycle_id,required_marks,duration_minutes,configured_by_user_id)
            SELECT SCOPE_IDENTITY(),@cycle,60,90,user_id FROM dbo.APP_USER WHERE email='hod@example.invalid';
            SELECT @cycle;
            """,db);
        int cycle = Convert.ToInt32(await seed.ExecuteScalarAsync());
        var before = await http.GetFromJsonAsync<CurrentExamResponse>("/api/exams/current");
        string route = $"/api/datesheets/cycles/{cycle}";
        var choices = await http.GetFromJsonAsync<JsonElement>(route+"/courses");
        int[] ids = choices.GetProperty("courses").EnumerateArray().Select(row => row.GetProperty("course_exam_id").GetInt32()).ToArray();
        Assert.HasCount(2,ids);
        var originalHistory = await History(http);
        using var damaged = new MultipartFormDataContent();
        damaged.Add(new ByteArrayContent([1,2,3]),"file","damaged.xlsx");
        Assert.AreEqual(HttpStatusCode.BadRequest,(await http.PostAsync(route+"/drafts",damaged)).StatusCode);
        http.DefaultRequestHeaders.Authorization = null;
        Assert.AreEqual(HttpStatusCode.Unauthorized,(await Upload(http,route,ids)).StatusCode);
        http.DefaultRequestHeaders.Authorization = new("Bearer",dataCell);

        var bad = await Upload(http,route,[ids[0],ids[0],int.MaxValue], true);
        Assert.AreEqual(HttpStatusCode.UnprocessableEntity,bad.StatusCode);
        var errors = (await bad.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        CollectionAssert.AreEquivalent(new[]{2,3,4},errors.EnumerateArray().Select(e=>e.GetProperty("row").GetInt32()).Distinct().ToArray());
        Assert.AreEqual(originalHistory.GetArrayLength(),(await History(http)).GetArrayLength());
        http.DefaultRequestHeaders.Authorization = new("Bearer",director);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Upload(http,route,ids)).StatusCode);
        http.DefaultRequestHeaders.Authorization = new("Bearer",dataCell);

        int incomplete = await Draft(http, route, [ids[0]]);
        var preview = await http.GetFromJsonAsync<JsonElement>($"/api/datesheets/{incomplete}/preview");
        Assert.AreEqual("MISSING_SCHEDULE",preview.GetProperty("errors")[0].GetProperty("validation_error").GetString());
        Assert.AreEqual(HttpStatusCode.Conflict,(await Publish(http,incomplete)).StatusCode);
        int full = await Draft(http,route,ids);
        preview = await http.GetFromJsonAsync<JsonElement>($"/api/datesheets/{full}/preview");
        Assert.AreEqual(2,preview.GetProperty("schedule").GetArrayLength());
        Assert.AreEqual(0,preview.GetProperty("errors").GetArrayLength());
        Assert.AreEqual("DRAFT",preview.GetProperty("header")[0].GetProperty("status").GetString());
        http.DefaultRequestHeaders.Authorization = new("Bearer",director);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Publish(http,full)).StatusCode);
        var unchanged = await http.GetFromJsonAsync<CurrentExamResponse>("/api/exams/current");
        Assert.AreEqual(before!.ActiveDatesheetId,unchanged!.ActiveDatesheetId);
        http.DefaultRequestHeaders.Authorization = new("Bearer",dataCell);
        var published = await Publish(http,full);
        published.EnsureSuccessStatusCode();
        var current = (await published.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("current")[0];
        Assert.AreEqual(cycle,current.GetProperty("active_exam_cycle_id").GetInt32());
        Assert.AreEqual(full,current.GetProperty("active_datesheet_id").GetInt32());
        Assert.AreEqual(HttpStatusCode.Conflict,(await Publish(http,full)).StatusCode);
        int revision = await Draft(http,route,ids);
        (await Publish(http,revision)).EnsureSuccessStatusCode();
        var history = await History(http);
        var previous = history.EnumerateArray().Single(row=>row.GetProperty("datesheet_id").GetInt32()==full);
        Assert.AreEqual("SUPERSEDED",previous.GetProperty("status").GetString());
        Assert.AreEqual(1,history.EnumerateArray().Count(row=>row.GetProperty("is_current").GetBoolean()));
        http.DefaultRequestHeaders.Authorization = new("Bearer",director);
        var after = await http.GetFromJsonAsync<CurrentExamResponse>("/api/exams/current");
        Assert.AreEqual(revision,after!.ActiveDatesheetId);
        Assert.AreEqual("PUBLISHED",after.DatesheetStatus);
        using var audit = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.AUDIT_EVENT WHERE entity_type='DATESHEET' AND action='PUBLISH_AND_SET_CURRENT'
            AND entity_key IN (CONVERT(nvarchar(200),@full),CONVERT(nvarchar(200),@revision),CONVERT(nvarchar(200),@incomplete));
            """,db);
        audit.Parameters.AddWithValue("@full",full);
        audit.Parameters.AddWithValue("@revision",revision);
        audit.Parameters.AddWithValue("@incomplete",incomplete);
        Assert.AreEqual(2,Convert.ToInt32(await audit.ExecuteScalarAsync()));
    }

    private static async Task<JsonElement> History(HttpClient http)
    {
        var result = await http.GetFromJsonAsync<JsonElement>("/api/datesheets/history");
        return result.GetProperty("history");
    }

    private static Task<HttpResponseMessage> Publish(HttpClient http,int id)
        => http.PostAsync($"/api/datesheets/{id}/publish",null);

    private static async Task<int> Draft(HttpClient http,string route,int[] ids)
    {
        using var result = await Upload(http,route,ids);
        Assert.IsTrue(result.IsSuccessStatusCode,await result.Content.ReadAsStringAsync());
        var draft = await result.Content.ReadFromJsonAsync<JsonElement>();
        return draft.GetProperty("draft")[0].GetProperty("datesheet_id").GetInt32();
    }

    private static async Task<HttpResponseMessage> Upload(HttpClient http,string route,int[] ids,bool invalid=false)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Datesheet");
        string[] headers = ["course_exam_id","exam_date","start_time","end_time"];
        for (int i=0;i<headers.Length;i++)
            sheet.Cell(1,i+1).Value=headers[i];
        for (int i=0;i<ids.Length;i++)
        {
            int row=i+2;
            sheet.Cell(row,1).Value=ids[i];
            sheet.Cell(row,2).Value=new DateTime(2028,4,10);
            sheet.Cell(row,3).Value=new TimeSpan(9,0,0);
            sheet.Cell(row,4).Value="11:00";
        }
        if (invalid) sheet.Cell(2,2).Value="bad date";
        using var bytes=new MemoryStream();
        workbook.SaveAs(bytes);
        using var form=new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes.ToArray()),"file","schedule.xlsx");
        return await http.PostAsync(route+"/drafts",form);
    }
}

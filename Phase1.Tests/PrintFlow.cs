using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using UglyToad.PdfPig;

namespace Phase1.Tests;

internal static class PrintFlow
{
    public static async Task AttachFixtureDiagram(HttpClient http, int version)
    {
        string path = $"/api/paper-versions/{version}";
        var workspace = await http.GetFromJsonAsync<JsonElement>(path);
        var last = workspace.GetProperty("questions").EnumerateArray().Last(q=>q.GetProperty("placement").GetString()=="MAIN");
        using var upload = new MultipartFormDataContent();
        upload.Add(new StringContent(workspace.GetProperty("header")[0].GetProperty("row_version").GetString()!),"rowVersion");
        upload.Add(new StringContent(last.GetProperty("question_version_id").GetInt32().ToString()),"questionVersionId");
        byte[] image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAIwAAAA8CAYAAAC0ClJLAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAKBSURBVHhe7ZwNzoMgDEA9ngfyON5lV9lN+ChOJ8J+ug/quryXdBEz3GZfChGyIQAoQBhQgTCgohBmGAbCKDxSFQb6gzCgAmEKLmEapvjaj8t0L+/jfL2d9QHCFOyEuc5hHOfQNKWXKQzbNeWzxuDJGYQp6CxMxjXM4xCmnuWsMQhTsAqzJFOuu1aE6zwu7d25VDGmKfa5nY/Zv7/v1dDWf/hrjfwujxgIE9lXmCTGPbUiRZp/yPkt6dL3Pi+Rucqj6rFKxRzGBnNh9hPVLcSGTKS8YmxSPUGu60madvfZllOEqSb2n8Lk/b8fhCmoC5MSe5Dhkwqz9btBhbHBRph0LMPP0k7JTu0Y1bnNexUmG94cVReh3X22paMw8AyEARUIAyoQBlQgDKj4KWEIm/AIFeYkEAZUIExB/vCtBzy4s8dGmA77YfKlAfbDWOFWmCOPlg++FYQpWIWx2EC1DE9UmP4YCBM5rlbvMrtVhmwVW/q+t4EqIX07V7DWIExBXZhsorqG2PDBanUiE80P7e6zLacIU03+J8I4lUVAmIK6MMckp3nKJxVGrulUFgFhCvZJl2MZfpZ2kiS1Y1TnNq+FqQ1tD4etL0S+r0c6CgPPQBhQgTCgAmFABcKAip8ShrAJj1BhTgJhQAXCFOQP3/ohn+Prz4QEhCnYCbNfGmjGbtsEwpjhWJgVEQdhrDAQpvcGKoSxxECYyHG1eltkXORJi4bZKrb0fXcDFcJYYi5M8w1UCGPKKcJUk48wLjAXJolxkIEK4wcbYdKxDD9LO0mS2jGqcxuE+VY6CgPPQBhQgTCgAmFABcKAip8ShrAJj1BOQAXCgAqEARUIAyoQBhSE8Adajg/E86VvrgAAAABJRU5ErkJggg==");
        upload.Add(new ByteArrayContent(image),"file","stack.png");
        (await http.PostAsync(path+"/attachments",upload)).EnsureSuccessStatusCode();
    }

    public static async Task VerifyAsync(HttpClient http, SqlConnection db, string director, string dataCell)
    {
        using var latest = new SqlCommand("""
            SELECT TOP(1) review_id,reviewed_version_id,reviewer_user_id FROM dbo.REVIEW
            WHERE review_stage='DIRECTOR' AND decision='APPROVED' ORDER BY review_id DESC
            """,db);
        using var reader = await latest.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        int approval=reader.GetInt32(0), version=reader.GetInt32(1), deliveredBy=reader.GetInt32(2);
        await reader.CloseAsync();
        string root=$"/api/approvals/{approval}";
        using var refused = new SqlCommand("SELECT MAX(review_id) FROM dbo.REVIEW WHERE decision='CHANGES_REQUESTED'",db);
        int changes=Convert.ToInt32(await refused.ExecuteScalarAsync());
        http.DefaultRequestHeaders.Authorization=null;
        Assert.AreEqual(HttpStatusCode.Unauthorized,(await http.PostAsync(root+"/print-exports",null)).StatusCode);
        http.DefaultRequestHeaders.Authorization=new("Bearer",director);
        Assert.AreEqual(HttpStatusCode.Conflict,(await http.PostAsync($"/api/approvals/{changes}/print-exports",null)).StatusCode);
        var workspace=await http.GetFromJsonAsync<JsonElement>($"/api/paper-versions/{version}");
        var export=await http.PostAsync(root+"/print-exports",null);
        Assert.IsTrue(export.IsSuccessStatusCode,$"Print failed: {export.StatusCode}");
        Assert.AreEqual("application/pdf",export.Content.Headers.ContentType!.MediaType);
        byte[] bytes=await export.Content.ReadAsByteArrayAsync();
        string id=export.Headers.GetValues("X-Print-Export-Id").Single();
        CollectionAssert.AreEqual(bytes,await http.GetByteArrayAsync(root+"/print-exports/"+id));
        using var pdf=PdfDocument.Open(bytes);
        string text=string.Join(" ",pdf.GetPages().SelectMany(page=>page.GetWords()).Select(word=>word.Text));
        StringAssert.Contains(text,"Acting Director wording");
        StringAssert.Contains(text,"Time limit is two hours.");
        int lastPosition=-1;
        foreach(var question in workspace.GetProperty("questions").EnumerateArray())
        {
            string prompt=question.GetProperty("prompt_text").GetString()!;
            int position=text.IndexOf(prompt,StringComparison.Ordinal);
            if(question.GetProperty("placement").GetString()=="MAIN")
            {
                Assert.IsGreaterThan(lastPosition,position,"Missing or unordered MAIN question: "+prompt);
                lastPosition=position;
            }
            else Assert.AreEqual(-1,position,"EXTRA appeared in PDF");
        }
        Assert.AreEqual(1,pdf.GetPages().Sum(page=>page.NumberOfImages));
        var sample=Environment.GetEnvironmentVariable("Phase8SamplePdf");
        if (!string.IsNullOrEmpty(sample)) await File.WriteAllBytesAsync(sample,bytes);
        using var receipts=new SqlCommand("SELECT COUNT(*) FROM dbo.PHYSICAL_RECEIPT WHERE approval_review_id=@approval",db);
        receipts.Parameters.AddWithValue("@approval",approval);
        Assert.AreEqual(0,Convert.ToInt32(await receipts.ExecuteScalarAsync()));
        var body=new {deliveredByUserId=deliveredBy,remarks="Sealed fixture packet"};
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PostAsJsonAsync(root+"/sealed-receipt",body)).StatusCode);
        http.DefaultRequestHeaders.Authorization=new("Bearer",dataCell);
        Assert.AreEqual(HttpStatusCode.Conflict,(await http.PostAsJsonAsync($"/api/approvals/{changes}/sealed-receipt",body)).StatusCode);
        var attempts=await Task.WhenAll(http.PostAsJsonAsync(root+"/sealed-receipt",body),http.PostAsJsonAsync(root+"/sealed-receipt",body));
        Assert.AreEqual(1,attempts.Count(result=>result.StatusCode==HttpStatusCode.Conflict));
        var saved=await attempts.Single(result=>result.IsSuccessStatusCode).Content.ReadFromJsonAsync<JsonElement>();
        var receipt=saved.GetProperty("receipt")[0];
        Assert.AreEqual("SEALED",receipt.GetProperty("seal_status").GetString());
        Assert.AreEqual(deliveredBy,receipt.GetProperty("delivered_by_user_id").GetInt32());
        Assert.AreEqual(approval,receipt.GetProperty("approval_review_id").GetInt32());
        Assert.AreEqual(1,Convert.ToInt32(await receipts.ExecuteScalarAsync()));
    }
}

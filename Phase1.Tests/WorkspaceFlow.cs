using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Phase1.Tests;

internal static class WorkspaceFlow
{
    public static async Task VerifyAsync(HttpClient http, SqlConnection db, int paper, int topic, int outsideTopic,
        int clo, string setter, string contributor, string other)
    {
        void As(string token) => http.DefaultRequestHeaders.Authorization = new("Bearer", token);
        string versions = $"/api/papers/{paper}/versions";
        As(contributor);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PostAsJsonAsync(versions,new {reason="First draft"})).StatusCode);
        As(setter);
        var version = await Result(await http.PostAsJsonAsync(versions,new {reason="First draft"}),"version");
        int id = version.GetProperty("paper_version_id").GetInt32();
        string root = $"/api/paper-versions/{id}";
        string token = Token(version);
        string originalToken = token;
        object Question(string prompt,string placement,int selectedTopic)
        {
            return new {
                rowVersion=token,prompt,placement,difficulty="MEDIUM",
                parts=new object[]{
                    new {key=1,kind="GROUP",label="Q",body="Answer both"},
                    new {key=2,parentKey=1,kind="ITEM",label="a",body="Explain",topicId=selectedTopic,marks=3,difficulty="EASY"},
                    new {key=3,parentKey=1,kind="ITEM",label="b",body="Compare",topicId=selectedTopic,marks=7,difficulty="MEDIUM"}
                },
                mappings=new object[]{new {examCloId=clo},new {partKey=2,examCloId=clo}}
            };
        }
        As(contributor);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PostAsJsonAsync(root+"/questions",Question("Denied","MAIN",topic))).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest,(await http.PostAsJsonAsync(root+"/questions",Question("Outside scope","EXTRA",outsideTopic))).StatusCode);
        var first = await Result(await http.PostAsJsonAsync(root+"/questions",Question("First stem","EXTRA",topic)),"saved");
        int item=first.GetProperty("paper_item_id").GetInt32();
        token=Token(first);
        As(setter);
        Assert.AreEqual(HttpStatusCode.Conflict,(await http.PutAsJsonAsync(root+"/instructions",new {rowVersion=originalToken,instructions="Stale"})).StatusCode);
        As(other);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PutAsJsonAsync($"{root}/items/{item}/question",Question("Wrong author","EXTRA",topic))).StatusCode);
        As(contributor);
        token=Token(await Result(await http.PutAsJsonAsync($"{root}/items/{item}/question",Question("Second stem","EXTRA",topic)),"saved"));
        var workspace = await http.GetFromJsonAsync<JsonElement>(root);
        Assert.AreEqual(0m,workspace.GetProperty("header")[0].GetProperty("added_marks").GetDecimal());
        int question=workspace.GetProperty("questions")[0].GetProperty("question_version_id").GetInt32();
        int part=workspace.GetProperty("parts")[1].GetProperty("question_part_id").GetInt32();
        Assert.AreEqual(2,workspace.GetProperty("cloMappings").GetArrayLength());
        var revisions=await http.GetFromJsonAsync<JsonElement>($"/api/question-versions/{question}/history");
        Assert.AreEqual("First stem",revisions.GetProperty("revisions")[1].GetProperty("prompt_text").GetString());
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PutAsJsonAsync($"{root}/items/{item}/placement",new {rowVersion=token,placement="MAIN"})).StatusCode);
        As(setter);
        token=Token(await Result(await http.PutAsJsonAsync($"{root}/items/{item}/placement",new {rowVersion=token,placement="MAIN"}),"saved"));
        As(contributor);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await http.PutAsJsonAsync($"{root}/items/{item}/question",Question("Own but MAIN","EXTRA",topic))).StatusCode);
        As(setter);
        var second=await Result(await http.PostAsJsonAsync(root+"/questions",Question("Another","MAIN",topic)),"saved");
        token=Token(second);
        int secondItem=second.GetProperty("paper_item_id").GetInt32();
        token=Token(await Result(await http.PutAsJsonAsync(root+"/order",new {rowVersion=token,placement="MAIN",itemIds=new[]{secondItem,item}}),"saved"));
        var concurrent = await Task.WhenAll(
            http.PutAsJsonAsync(root+"/instructions",new {rowVersion=token,instructions="Attempt both"}),
            http.PutAsJsonAsync(root+"/instructions",new {rowVersion=token,instructions="Answer both"}));
        var winner = concurrent.Single(response=>response.StatusCode==HttpStatusCode.OK);
        Assert.AreEqual(HttpStatusCode.Conflict,concurrent.Single(response=>response!=winner).StatusCode);
        token=Token(await Result(winner,"saved"));
        workspace=await http.GetFromJsonAsync<JsonElement>(root);
        Assert.AreEqual(20m,workspace.GetProperty("header")[0].GetProperty("added_marks").GetDecimal());
        Assert.AreEqual(secondItem,workspace.GetProperty("questions")[0].GetProperty("paper_item_id").GetInt32());

        byte[] png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jx1sAAAAASUVORK5CYII=");
        async Task<HttpResponseMessage> Attach(string path,string expected,byte[] bytes,int? targetPart=null)
        {
            using var form=new MultipartFormDataContent();
            form.Add(new StringContent(expected),"rowVersion");
            form.Add(new StringContent(question.ToString()),"questionVersionId");
            form.Add(new StringContent((targetPart ?? part).ToString()),"partId");
            form.Add(new ByteArrayContent(bytes),"file","figure.png");
            return await http.PostAsync(path+"/attachments",form);
        }
        Assert.AreEqual(HttpStatusCode.BadRequest,(await Attach(root,token,[1,2,3])).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest,(await Attach(root,token,png,int.MaxValue)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict,(await Attach(root,originalToken,png)).StatusCode);
        As(contributor);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Attach(root,token,png)).StatusCode);
        As(setter);
        var linked=await Result(await Attach(root,token,png),"saved");
        int attachment=linked.GetProperty("attachment_id").GetInt32();
        CollectionAssert.AreEqual(png,await http.GetByteArrayAsync($"{root}/attachments/{attachment}"));
        var files=await http.GetFromJsonAsync<JsonElement>(root+"/attachments");
        Assert.IsFalse(files.GetProperty("attachments")[0].TryGetProperty("storage_key",out _));
        http.DefaultRequestHeaders.Authorization=null;
        Assert.AreEqual(HttpStatusCode.Unauthorized,(await http.GetAsync($"{root}/attachments/{attachment}")).StatusCode);
        As(setter);

        // Disposable fixture ko correction state mein rakh kar frozen version dobara parhte hain.
        using var freeze=new SqlCommand("""
            UPDATE dbo.PAPER_VERSION SET frozen_at=SYSUTCDATETIME() WHERE paper_version_id=@version;
            UPDATE dbo.PAPER SET workflow_status='CHANGES_REQUESTED' WHERE paper_id=@paper;
            """,db);
        freeze.Parameters.AddWithValue("@version",id);
        freeze.Parameters.AddWithValue("@paper",paper);
        await freeze.ExecuteNonQueryAsync();
        var correction=await Result(await http.PostAsJsonAsync(versions,new {reason="Correction"}),"version");
        string next=$"/api/paper-versions/{correction.GetProperty("paper_version_id").GetInt32()}";
        token=Token(correction);
        Assert.AreEqual(HttpStatusCode.Conflict,(await Attach(next,token,png)).StatusCode);
        var copied=await http.GetFromJsonAsync<JsonElement>(next);
        int copiedItem=copied.GetProperty("questions")[1].GetProperty("paper_item_id").GetInt32();
        var edit=await Result(await http.PutAsJsonAsync($"{next}/items/{copiedItem}/question",Question("Corrected stem","MAIN",topic)),"saved");
        var newFiles=await http.GetFromJsonAsync<JsonElement>(next+"/attachments");
        var remove=new HttpRequestMessage(HttpMethod.Delete,$"{next}/attachments/{newFiles.GetProperty("attachments")[0].GetProperty("attachment_id").GetInt32()}")
        {Content=JsonContent.Create(new {rowVersion=Token(edit)})};
        (await http.SendAsync(remove)).EnsureSuccessStatusCode();
        var old=await http.GetFromJsonAsync<JsonElement>(root);
        Assert.AreEqual("Second stem",old.GetProperty("questions")[1].GetProperty("prompt_text").GetString());
        CollectionAssert.AreEqual(png,await http.GetByteArrayAsync($"{root}/attachments/{attachment}"));
        var history=await http.GetFromJsonAsync<JsonElement>(versions);
        Assert.AreEqual(2,history.GetProperty("versions").GetArrayLength());
    }

    private static string Token(JsonElement row) => row.GetProperty("row_version").GetString()!;

    private static async Task<JsonElement> Result(HttpResponseMessage response,string name)
    {
        Assert.IsTrue(response.IsSuccessStatusCode,$"{response.RequestMessage?.RequestUri}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        var result=await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty(name)[0];
    }
}

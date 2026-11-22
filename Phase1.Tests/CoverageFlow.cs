using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Phase1.Tests;

internal static class CoverageFlow
{
    public static async Task VerifyAsync(HttpClient http, SqlConnection db, string director,
        string hod, string firstTeacher, string secondTeacher)
    {
        // Disposable fixture: teen sections, aik offering, aik exam aur do outline topics.
        using var seed = new SqlCommand("""
            DECLARE @course int,@dept int,@outline int,@hod int,@session int,@offering int,@cycle int,@exam int;
            SELECT @course=c.course_id,@dept=c.owner_department_id,@outline=o.outline_id FROM dbo.COURSE c
            JOIN dbo.COURSE_OUTLINE o ON o.course_id=c.course_id WHERE c.course_code='CS-301' AND o.version_number=1;
            SELECT @hod=user_id FROM dbo.APP_USER WHERE staff_number='DEMO-HOD';
            INSERT dbo.ACADEMIC_SESSION(session_name,starts_on,ends_on) VALUES('Coverage Test','2030-01-01','2030-06-30');
            SET @session=SCOPE_IDENTITY();
            INSERT dbo.EXAM_CYCLE(session_id,exam_type) VALUES(@session,'Final');
            SET @cycle=SCOPE_IDENTITY();
            INSERT dbo.COURSE_OFFERING(course_id,outline_id,session_id,lead_department_id) VALUES(@course,@outline,@session,@dept);
            SET @offering=SCOPE_IDENTITY();
            INSERT dbo.SECTION(program_id,session_id,section_name,semester_number)
            SELECT p.program_id,@session,v.name,1 FROM dbo.PROGRAM p CROSS JOIN (VALUES('A'),('B'),('C')) v(name)
            WHERE p.program_code='BSCS';
            INSERT dbo.OFFERING_SECTION(offering_id,section_id) SELECT @offering,section_id FROM dbo.SECTION WHERE session_id=@session;
            INSERT dbo.USER_ROLE(user_id,role_id,department_id,valid_from)
            SELECT @hod,role_id,@dept,DATEADD(day,-1,SYSUTCDATETIME()) FROM dbo.ROLE WHERE role_code='TEACHER';
            INSERT dbo.TEACHING_ASSIGNMENT(offering_section_id,teacher_user_id,starts_at)
            SELECT os.offering_section_id,u.user_id,DATEADD(day,-1,SYSUTCDATETIME()) FROM dbo.OFFERING_SECTION os
            JOIN dbo.SECTION s ON s.section_id=os.section_id JOIN dbo.APP_USER u ON u.staff_number=
              CASE s.section_name WHEN 'A' THEN 'DEMO-T1' WHEN 'B' THEN 'DEMO-T2' ELSE 'DEMO-HOD' END
            WHERE os.offering_id=@offering;
            INSERT dbo.COURSE_EXAM(offering_id,exam_cycle_id,required_marks,duration_minutes,configured_by_user_id)
            VALUES(@offering,@cycle,60,90,@hod);
            SET @exam=SCOPE_IDENTITY();
            INSERT dbo.EXAM_CLO(course_exam_id,clo_id) SELECT @exam,clo_id FROM dbo.CLO WHERE outline_id=@outline;
            INSERT dbo.USER_ROLE(user_id,role_id,department_id,valid_from)
            SELECT u.user_id,r.role_id,d.department_id,DATEADD(day,-1,SYSUTCDATETIME())
            FROM dbo.APP_USER u CROSS JOIN dbo.ROLE r CROSS JOIN dbo.DEPARTMENT d
            WHERE u.staff_number='DEMO-T2' AND r.role_code='HOD' AND d.department_code='BIO';
            SELECT @exam;
            """, db);
        int exam = Convert.ToInt32(await seed.ExecuteScalarAsync());
        string root = $"/api/exams/{exam}";
        string coverage = root + "/coverage";
        void As(string token) => http.DefaultRequestHeaders.Authorization = new("Bearer",token);
        As(hod);
        var initial = await http.GetFromJsonAsync<JsonElement>(coverage);
        var sections = initial.GetProperty("sections").EnumerateArray().OrderBy(s=>s.GetProperty("section_name").GetString()).ToArray();
        Assert.HasCount(3,sections);
        int[] assignments = sections.Select(s=>s.GetProperty("teaching_assignment_id").GetInt32()).ToArray();
        int[] teachers = sections.Select(s=>s.GetProperty("teacher_user_id").GetInt32()).ToArray();
        int[] topics = initial.GetProperty("matrix").EnumerateArray().Select(t=>t.GetProperty("topic_id").GetInt32()).Distinct().Order().ToArray();
        Assert.HasCount(2,topics);
        Assert.IsTrue(sections.All(s=>s.GetProperty("coverage_submission_id").ValueKind==JsonValueKind.Null));
        Task<HttpResponseMessage> Save(int assignment,bool submit,params int[] selected)
            => http.PutAsJsonAsync($"{coverage}/assignments/{assignment}",new {submit,topics=selected.Select(topicId=>new {topicId,status="COVERED"})});
        Task<HttpResponseMessage> Lock() => http.PostAsync(coverage+"/lock",null);
        Task<HttpResponseMessage> Assign(int teacherId) => http.PostAsJsonAsync(root+"/setter-assignments", new {teacherId,dueAt=DateTimeOffset.UtcNow.AddDays(7)});
        http.DefaultRequestHeaders.Authorization = null;
        Assert.AreEqual(HttpStatusCode.Unauthorized,(await http.GetAsync(coverage)).StatusCode);
        As(firstTeacher);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Save(assignments[1],true,topics)).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest,(await Save(assignments[0],true,topics[0],topics[0])).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest,(await Save(assignments[0],true,int.MaxValue)).StatusCode);
        (await Save(assignments[0],false,topics)).EnsureSuccessStatusCode();
        (await Save(assignments[0],true,topics)).EnsureSuccessStatusCode();
        As(secondTeacher);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Assign(teachers[1])).StatusCode);
        (await Save(assignments[1],true,topics[0])).EnsureSuccessStatusCode();
        As(hod);
        Assert.AreEqual(HttpStatusCode.Conflict,(await Lock()).StatusCode);
        var empty = await Save(assignments[2],true);
        empty.EnsureSuccessStatusCode();
        var row=(await empty.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sections")[0];
        Assert.AreEqual("SUBMITTED",row.GetProperty("submission_status").GetString());
        Assert.IsGreaterThan(0,row.GetProperty("coverage_submission_id").GetInt32());
        Assert.AreEqual(HttpStatusCode.Conflict,(await Lock()).StatusCode);
        (await Save(assignments[2],true,topics[1])).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Conflict,(await Lock()).StatusCode);
        (await Save(assignments[2],true,topics)).EnsureSuccessStatusCode();
        As(director);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Lock()).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden,(await Assign(teachers[0])).StatusCode);
        As(hod);
        (await Lock()).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Conflict,(await Save(assignments[2],true)).StatusCode);
        (await Assign(teachers[0])).EnsureSuccessStatusCode();
        (await Assign(teachers[1])).EnsureSuccessStatusCode();
        var final = await http.GetFromJsonAsync<JsonElement>(coverage);
        var locked = final.GetProperty("lockedTopics");
        Assert.AreEqual(1,locked.GetArrayLength());
        Assert.AreEqual(topics[0],locked[0].GetProperty("topic_id").GetInt32());
        var history=final.GetProperty("setterHistory");
        Assert.AreEqual(2,history.GetArrayLength());
        Assert.AreEqual(teachers[1],history[0].GetProperty("setter_user_id").GetInt32());
        Assert.AreEqual(JsonValueKind.Null,history[0].GetProperty("ended_at").ValueKind);
        Assert.AreNotEqual(JsonValueKind.Null,history[1].GetProperty("ended_at").ValueKind);
        Assert.AreEqual(history[0].GetProperty("paper_id").GetInt32(),history[1].GetProperty("paper_id").GetInt32());
        using var participants = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.PAPER_PARTICIPANT pp JOIN dbo.PAPER p ON p.paper_id=pp.paper_id WHERE p.course_exam_id=@exam
            """,db);
        participants.Parameters.AddWithValue("@exam",exam);
        Assert.AreEqual(3,Convert.ToInt32(await participants.ExecuteScalarAsync()));
        As(director);
    }
}

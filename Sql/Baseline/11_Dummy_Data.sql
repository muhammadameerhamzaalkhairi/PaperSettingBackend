USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- OPTIONAL DEMO ONLY. Run once on the fresh Tables.sql database, after 01..09.
-- Uses fake staff and unusable placeholder hashes; these are NOT real login credentials.
-- Deliberate historical fixture: Spring paper is approved to demonstrate Fall exact reuse.
-- No ROLE inserts: Tables.sql already seeded the four application roles.
IF EXISTS(SELECT 1 FROM dbo.APP_USER) THROW 51100,'Demo requires an empty staff database.',1;
INSERT dbo.DEPARTMENT(department_code,department_name) VALUES(N'CS',N'Computer Science');
DECLARE @Dept int=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.PROGRAM(department_id,program_code,program_name) VALUES(@Dept,N'BSCS',N'BS Computer Science');
DECLARE @Program int=CONVERT(int,SCOPE_IDENTITY());
DECLARE @Hash nvarchar(256)=N'DEMO_ONLY_NOT_A_VALID_ASPNET_PASSWORD_HASH';
INSERT dbo.APP_USER(staff_number,full_name,email,password_hash,home_department_id,designation) VALUES
(N'DEMO-DIR',N'Demo Director',N'director@example.invalid',@Hash,@Dept,N'Director'),
(N'DEMO-HOD',N'Demo HOD',N'hod@example.invalid',@Hash,@Dept,N'HOD'),
(N'DEMO-DC',N'Demo Data Cell',N'datacell@example.invalid',@Hash,@Dept,N'Data Cell Officer'),
(N'DEMO-T1',N'Amir Rasheed',N'amir@example.invalid',@Hash,@Dept,N'Lecturer'),
(N'DEMO-T2',N'Saeed Iqbal',N'saeed@example.invalid',@Hash,@Dept,N'Lecturer');
DECLARE @Director int=(SELECT user_id FROM dbo.APP_USER WHERE staff_number=N'DEMO-DIR'),
@Hod int=(SELECT user_id FROM dbo.APP_USER WHERE staff_number=N'DEMO-HOD'),
@DataCell int=(SELECT user_id FROM dbo.APP_USER WHERE staff_number=N'DEMO-DC'),
@Teacher int=(SELECT user_id FROM dbo.APP_USER WHERE staff_number=N'DEMO-T1'),
@Teacher2 int=(SELECT user_id FROM dbo.APP_USER WHERE staff_number=N'DEMO-T2');
INSERT dbo.USER_ROLE(user_id,role_id,department_id,valid_from)
SELECT u.user_id,r.role_id,CASE WHEN r.role_code IN(N'HOD',N'TEACHER') THEN @Dept END,DATEADD(day,-1,SYSUTCDATETIME())
FROM dbo.APP_USER u JOIN dbo.ROLE r ON r.role_code=CASE u.staff_number WHEN N'DEMO-DIR' THEN N'DIRECTOR' WHEN N'DEMO-HOD' THEN N'HOD' WHEN N'DEMO-DC' THEN N'DATA_CELL' ELSE N'TEACHER' END;
INSERT dbo.COURSE(owner_department_id,course_code,course_title) VALUES(@Dept,N'CS-301',N'Data Structures');
DECLARE @Course int=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.COURSE_OUTLINE(course_id,version_number,published_at) VALUES(@Course,1,SYSUTCDATETIME());
DECLARE @Outline int=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.TOPIC(outline_id,topic_title,sort_order) VALUES(@Outline,N'Linear Data Structures',1);
DECLARE @Topic int=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.TOPIC(outline_id,topic_title,parent_topic_id,sort_order) VALUES(@Outline,N'Stacks',@Topic,1);
INSERT dbo.CLO(outline_id,clo_code,clo_statement) VALUES(@Outline,N'CLO-1',N'Explain fundamental linear data structures and their operations.');
DECLARE @Clo int=CONVERT(int,SCOPE_IDENTITY());
DECLARE @Session int,@Cycle int,@Offering int,@Exam int,@ExamClo int,@Sec int,@OS int,@TA int,@TA2 int,
@Paper int,@Version int,@Token binary(8),@Parts nvarchar(max),@Maps nvarchar(max),@Topics nvarchar(max),@Schedule nvarchar(max),@DateSheet int,@Submission int,@Due datetime2(0)=DATEADD(day,30,SYSUTCDATETIME());
DECLARE @MappingId int;

-- Spring 2026: one course, two sections, two teachers, one common paper.
INSERT dbo.ACADEMIC_SESSION(session_name,starts_on,ends_on) VALUES(N'Spring 2026','2026-01-01','2026-06-30');
SET @Session=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.EXAM_CYCLE(session_id,exam_type) VALUES(@Session,N'Final'); SET @Cycle=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.COURSE_OFFERING(course_id,outline_id,session_id,lead_department_id) VALUES(@Course,@Outline,@Session,@Dept); SET @Offering=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.SECTION(program_id,session_id,section_name,semester_number) VALUES(@Program,@Session,N'BSCS-5A',5); SET @Sec=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.OFFERING_SECTION(offering_id,section_id) VALUES(@Offering,@Sec); SET @OS=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.TEACHING_ASSIGNMENT(offering_section_id,teacher_user_id,starts_at) VALUES(@OS,@Teacher,DATEADD(day,-1,SYSUTCDATETIME())); SET @TA=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.SECTION(program_id,session_id,section_name,semester_number) VALUES(@Program,@Session,N'BSCS-5B',5); SET @Sec=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.OFFERING_SECTION(offering_id,section_id) VALUES(@Offering,@Sec); SET @OS=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.TEACHING_ASSIGNMENT(offering_section_id,teacher_user_id,starts_at) VALUES(@OS,@Teacher2,DATEADD(day,-1,SYSUTCDATETIME())); SET @TA2=CONVERT(int,SCOPE_IDENTITY());
EXEC dbo.usp_ConfigureCourseExam @ActorId=@Hod,@OfferingId=@Offering,@ExamCycleId=@Cycle,@Marks=40,@Duration=120;
SELECT @Exam=course_exam_id FROM dbo.COURSE_EXAM WHERE offering_id=@Offering AND exam_cycle_id=@Cycle;
SET @Maps=CONCAT(N'[',@Clo,N']'); EXEC dbo.usp_SetExamCLOs @Hod,@Exam,@Maps;
SELECT @ExamClo=exam_clo_id FROM dbo.EXAM_CLO WHERE course_exam_id=@Exam AND clo_id=@Clo;
SET @Topics=(SELECT @Topic AS topic_id,N'COVERED' AS status FOR JSON PATH);
EXEC dbo.usp_SaveCoverage @Teacher,@Exam,@TA,@Topics,1;
EXEC dbo.usp_SaveCoverage @Teacher2,@Exam,@TA2,@Topics,1;
EXEC dbo.usp_LockCommonTopics @Hod,@Exam;
EXEC dbo.usp_AssignPaperSetter @Hod,@Exam,@Teacher,@Due;
SELECT @Paper=paper_id FROM dbo.PAPER WHERE course_exam_id=@Exam;
EXEC dbo.usp_StartPaperRevision @Teacher,@Paper,N'Initial demo draft';
SELECT @Version=paper_version_id FROM dbo.PAPER_VERSION WHERE paper_id=@Paper;
SET @Parts=(SELECT 1 AS k,N'ITEM' AS kind,N'MAIN' AS label,N'' AS body,@Topic AS topic,10 AS marks,N'EASY' AS difficulty FOR JSON PATH);
SET @Maps=(SELECT @ExamClo AS exam_clo_id FOR JSON PATH);
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SaveQuestion @ActorId=@Teacher,@VersionId=@Version,@ExpectedRowVersion=@Token,@Prompt=N'Explain a stack and its operations.',@Difficulty=N'EASY',@PartsJson=@Parts,@CloMappingsJson=@Maps,@Placement=N'MAIN';
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SaveQuestion @ActorId=@Teacher,@VersionId=@Version,@ExpectedRowVersion=@Token,@Prompt=N'Explain a queue and its operations.',@Difficulty=N'EASY',@PartsJson=@Parts,@CloMappingsJson=@Maps,@Placement=N'MAIN';
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SaveQuestion @ActorId=@Teacher,@VersionId=@Version,@ExpectedRowVersion=@Token,@Prompt=N'Explain a linked list and its operations.',@Difficulty=N'EASY',@PartsJson=@Parts,@CloMappingsJson=@Maps,@Placement=N'MAIN';
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SaveQuestion @ActorId=@Teacher,@VersionId=@Version,@ExpectedRowVersion=@Token,@Prompt=N'Explain arrays and their operations.',@Difficulty=N'EASY',@PartsJson=@Parts,@CloMappingsJson=@Maps,@Placement=N'MAIN';
-- Fixture checks are explicit human demo decisions, not claims of automated AI validation.
DECLARE DemoMappings CURSOR LOCAL FAST_FORWARD FOR SELECT m.mapping_id FROM dbo.CLO_MAPPING m JOIN dbo.PAPER_ITEM i ON i.question_version_id=m.question_version_id WHERE i.paper_version_id=@Version;
OPEN DemoMappings; FETCH NEXT FROM DemoMappings INTO @MappingId;
WHILE @@FETCH_STATUS=0 BEGIN
 EXEC dbo.usp_ReviewCLOMapping @Hod,@MappingId,N'ALIGNED',N'Demo: this question explains a fundamental linear structure.';
 FETCH NEXT FROM DemoMappings INTO @MappingId;
END;
CLOSE DemoMappings; DEALLOCATE DemoMappings;
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SubmitPaper @Teacher,@Version,@Token;
SELECT @Submission=submission_id FROM dbo.PAPER_SUBMISSION WHERE paper_version_id=@Version;
EXEC dbo.usp_ReviewPaper @ActorId=@Hod,@SubmissionId=@Submission,@Stage=N'HOD',@Decision=N'APPROVED';
EXEC dbo.usp_ReviewPaper @ActorId=@Director,@SubmissionId=@Submission,@Stage=N'DIRECTOR',@Decision=N'APPROVED';

-- Fall 2026: one course, two sections, two teachers, one common paper.
INSERT dbo.ACADEMIC_SESSION(session_name,starts_on,ends_on) VALUES(N'Fall 2026','2026-08-01','2026-12-31');
SET @Session=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.EXAM_CYCLE(session_id,exam_type) VALUES(@Session,N'Final'); SET @Cycle=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.COURSE_OFFERING(course_id,outline_id,session_id,lead_department_id) VALUES(@Course,@Outline,@Session,@Dept); SET @Offering=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.SECTION(program_id,session_id,section_name,semester_number) VALUES(@Program,@Session,N'BSCS-5A',5); SET @Sec=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.OFFERING_SECTION(offering_id,section_id) VALUES(@Offering,@Sec); SET @OS=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.TEACHING_ASSIGNMENT(offering_section_id,teacher_user_id,starts_at) VALUES(@OS,@Teacher,DATEADD(day,-1,SYSUTCDATETIME())); SET @TA=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.SECTION(program_id,session_id,section_name,semester_number) VALUES(@Program,@Session,N'BSCS-5B',5); SET @Sec=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.OFFERING_SECTION(offering_id,section_id) VALUES(@Offering,@Sec); SET @OS=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.TEACHING_ASSIGNMENT(offering_section_id,teacher_user_id,starts_at) VALUES(@OS,@Teacher2,DATEADD(day,-1,SYSUTCDATETIME())); SET @TA2=CONVERT(int,SCOPE_IDENTITY());
EXEC dbo.usp_ConfigureCourseExam @ActorId=@Hod,@OfferingId=@Offering,@ExamCycleId=@Cycle,@Marks=40,@Duration=120;
SELECT @Exam=course_exam_id FROM dbo.COURSE_EXAM WHERE offering_id=@Offering AND exam_cycle_id=@Cycle;
SET @Maps=CONCAT(N'[',@Clo,N']'); EXEC dbo.usp_SetExamCLOs @Hod,@Exam,@Maps;
SELECT @ExamClo=exam_clo_id FROM dbo.EXAM_CLO WHERE course_exam_id=@Exam AND clo_id=@Clo;
SET @Topics=(SELECT @Topic AS topic_id,N'COVERED' AS status FOR JSON PATH);
EXEC dbo.usp_SaveCoverage @Teacher,@Exam,@TA,@Topics,1;
EXEC dbo.usp_SaveCoverage @Teacher2,@Exam,@TA2,@Topics,1;
EXEC dbo.usp_LockCommonTopics @Hod,@Exam;
EXEC dbo.usp_AssignPaperSetter @Hod,@Exam,@Teacher,@Due;
SELECT @Paper=paper_id FROM dbo.PAPER WHERE course_exam_id=@Exam;
EXEC dbo.usp_StartPaperRevision @Teacher,@Paper,N'Initial demo draft';
SELECT @Version=paper_version_id FROM dbo.PAPER_VERSION WHERE paper_id=@Paper;
SET @Parts=(SELECT 1 AS k,N'ITEM' AS kind,N'MAIN' AS label,N'' AS body,@Topic AS topic,10 AS marks,N'EASY' AS difficulty FOR JSON PATH);
SET @Maps=(SELECT @ExamClo AS exam_clo_id FOR JSON PATH);
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SaveQuestion @ActorId=@Teacher,@VersionId=@Version,@ExpectedRowVersion=@Token,@Prompt=N'Explain a stack and its operations.',@Difficulty=N'EASY',@PartsJson=@Parts,@CloMappingsJson=@Maps,@Placement=N'MAIN';
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SaveQuestion @ActorId=@Teacher,@VersionId=@Version,@ExpectedRowVersion=@Token,@Prompt=N'Explain a queue and its operations.',@Difficulty=N'EASY',@PartsJson=@Parts,@CloMappingsJson=@Maps,@Placement=N'MAIN';
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SaveQuestion @ActorId=@Teacher,@VersionId=@Version,@ExpectedRowVersion=@Token,@Prompt=N'Explain a linked list and its operations.',@Difficulty=N'EASY',@PartsJson=@Parts,@CloMappingsJson=@Maps,@Placement=N'MAIN';
SELECT @Token=row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version;
EXEC dbo.usp_SaveQuestion @ActorId=@Teacher,@VersionId=@Version,@ExpectedRowVersion=@Token,@Prompt=N'Compare a stack with a queue.',@Difficulty=N'EASY',@PartsJson=@Parts,@CloMappingsJson=@Maps,@Placement=N'MAIN';
SET @Schedule=(SELECT @Exam AS course_exam_id,CONVERT(date,'2026-12-15') AS exam_date,CONVERT(time(0),'09:00') AS start_time,CONVERT(time(0),'11:00') AS end_time FOR JSON PATH);
EXEC dbo.usp_SaveDatesheetDraft @DataCell,@Cycle,@Schedule;
SELECT @DateSheet=MAX(datesheet_id) FROM dbo.DATESHEET WHERE exam_cycle_id=@Cycle;
EXEC dbo.usp_PublishDatesheet @DataCell,@DateSheet;
EXEC dbo.usp_RunExactSimilarity @Teacher,@Version;
DECLARE @Run int=(SELECT MAX(similarity_run_id) FROM dbo.SIMILARITY_RUN WHERE paper_version_id=@Version);
EXEC dbo.usp_GetSimilarityReport @Teacher,@Run;
-- Expected: Spring 2026 Final, 3 matching current questions out of 4, 75.00%.

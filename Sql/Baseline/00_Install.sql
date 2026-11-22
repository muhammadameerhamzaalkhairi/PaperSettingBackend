-- ONE-FILE INSTALL: existing Tables.sql v2 database required. Run once.
-- Do not also execute the modular 01..10 files after this.

-- MODULE: 01_Views.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Internal views: expose them through actor-scoped procedures, not direct client access.
CREATE VIEW dbo.vw_CurrentExam AS
SELECT ss.settings_id,ss.active_exam_cycle_id,ss.active_datesheet_id,ec.session_id,
 a.session_name,ec.exam_type,d.revision_number,d.status AS datesheet_status,ss.updated_at
FROM dbo.SYSTEM_SETTINGS ss
LEFT JOIN dbo.EXAM_CYCLE ec ON ec.exam_cycle_id=ss.active_exam_cycle_id
LEFT JOIN dbo.ACADEMIC_SESSION a ON a.session_id=ec.session_id
LEFT JOIN dbo.DATESHEET d ON d.datesheet_id=ss.active_datesheet_id;
GO
CREATE VIEW dbo.vw_ActiveUserRole AS
SELECT ur.user_role_id,ur.user_id,r.role_code,ur.department_id
FROM dbo.USER_ROLE ur JOIN dbo.ROLE r ON r.role_id=ur.role_id
JOIN dbo.APP_USER u ON u.user_id=ur.user_id
WHERE u.is_active=1 AND ur.valid_from<=SYSUTCDATETIME()
AND (ur.valid_until IS NULL OR ur.valid_until>SYSUTCDATETIME());
GO
CREATE VIEW dbo.vw_CourseExam AS
SELECT ce.course_exam_id,ce.offering_id,ce.exam_cycle_id,ce.required_marks,ce.duration_minutes,
 ce.scope_locked_at,o.course_id,o.outline_id,o.lead_department_id,ec.session_id,
 c.course_code,c.course_title,a.session_name,ec.exam_type
FROM dbo.COURSE_EXAM ce JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id
JOIN dbo.COURSE c ON c.course_id=o.course_id
JOIN dbo.EXAM_CYCLE ec ON ec.exam_cycle_id=ce.exam_cycle_id
JOIN dbo.ACADEMIC_SESSION a ON a.session_id=ec.session_id;
GO
CREATE VIEW dbo.vw_RequiredCoverage AS
SELECT ce.course_exam_id,ta.teaching_assignment_id,ta.teacher_user_id,os.section_id,
 s.section_name,u.full_name,cs.coverage_submission_id,
 COALESCE(cs.submission_status,N'DRAFT') AS submission_status,cs.submitted_at
FROM dbo.COURSE_EXAM ce
JOIN dbo.OFFERING_SECTION os ON os.offering_id=ce.offering_id
JOIN dbo.SECTION s ON s.section_id=os.section_id
JOIN dbo.TEACHING_ASSIGNMENT ta ON ta.offering_section_id=os.offering_section_id
JOIN dbo.APP_USER u ON u.user_id=ta.teacher_user_id
LEFT JOIN dbo.TOPIC_COVERAGE_SUBMISSION cs ON cs.teaching_assignment_id=ta.teaching_assignment_id AND cs.course_exam_id=ce.course_exam_id
WHERE ta.starts_at<=SYSUTCDATETIME() AND (ta.ends_at IS NULL OR ta.ends_at>SYSUTCDATETIME());
GO
CREATE VIEW dbo.vw_ComputedCommonTopic AS
SELECT ce.course_exam_id,t.topic_id,t.topic_title
FROM dbo.vw_CourseExam ce JOIN dbo.TOPIC t ON t.outline_id=ce.outline_id
WHERE EXISTS(SELECT 1 FROM dbo.vw_RequiredCoverage r WHERE r.course_exam_id=ce.course_exam_id)
AND NOT EXISTS(SELECT 1 FROM dbo.OFFERING_SECTION os WHERE os.offering_id=ce.offering_id
 AND NOT EXISTS(SELECT 1 FROM dbo.vw_RequiredCoverage r WHERE r.course_exam_id=ce.course_exam_id AND r.section_id=os.section_id))
AND NOT EXISTS(SELECT 1 FROM dbo.vw_RequiredCoverage r WHERE r.course_exam_id=ce.course_exam_id
 AND (r.submission_status<>N'SUBMITTED' OR NOT EXISTS(
 SELECT 1 FROM dbo.TOPIC_COVERAGE tc WHERE tc.course_exam_id=ce.course_exam_id
 AND tc.teaching_assignment_id=r.teaching_assignment_id AND tc.topic_id=t.topic_id AND tc.coverage_status=N'COVERED')));
GO
CREATE VIEW dbo.vw_PaperVersionSummary AS
SELECT v.paper_version_id,v.paper_id,v.revision_number,v.frozen_at,v.required_marks_snapshot,
 v.duration_minutes_snapshot,v.row_version,
 (SELECT COUNT(*) FROM dbo.PAPER_ITEM i WHERE i.paper_version_id=v.paper_version_id AND i.placement=N'MAIN') AS main_question_count,
 COALESCE((SELECT SUM(qp.marks) FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_PART qp ON qp.question_version_id=i.question_version_id
 WHERE i.paper_version_id=v.paper_version_id AND i.placement=N'MAIN' AND qp.part_kind=N'ITEM'),0) AS added_marks
FROM dbo.PAPER_VERSION v;
GO
CREATE VIEW dbo.vw_LatestCLOCheck AS
SELECT c.check_id,c.mapping_id,c.check_status,c.explanation,c.checker_version,c.checked_at
FROM dbo.CLO_MAPPING_CHECK c
WHERE NOT EXISTS(SELECT 1 FROM dbo.CLO_MAPPING_CHECK n WHERE n.mapping_id=c.mapping_id AND n.check_id>c.check_id);
GO
CREATE VIEW dbo.vw_PaperList AS
SELECT p.paper_id,p.course_exam_id,p.workflow_status,p.archived_at,p.deleted_at,
 ce.course_code,ce.course_title,ce.session_name,ce.exam_type,ce.exam_cycle_id,ce.lead_department_id,
 pa.setter_user_id,u.full_name AS setter_name,pa.due_at,v.paper_version_id,v.revision_number,v.frozen_at
FROM dbo.PAPER p JOIN dbo.vw_CourseExam ce ON ce.course_exam_id=p.course_exam_id
LEFT JOIN dbo.PAPER_ASSIGNMENT pa ON pa.paper_id=p.paper_id AND pa.ended_at IS NULL
LEFT JOIN dbo.APP_USER u ON u.user_id=pa.setter_user_id
OUTER APPLY(SELECT TOP(1) pv.paper_version_id,pv.revision_number,pv.frozen_at FROM dbo.PAPER_VERSION pv
 WHERE pv.paper_id=p.paper_id ORDER BY pv.revision_number DESC) v;
GO
CREATE VIEW dbo.vw_DatesheetHistory AS
SELECT d.datesheet_id,d.exam_cycle_id,a.session_name,ec.exam_type,d.revision_number,
 d.status,d.published_at,d.published_by_user_id,
 CONVERT(bit,CASE WHEN ss.active_datesheet_id=d.datesheet_id THEN 1 ELSE 0 END) AS is_current
FROM dbo.DATESHEET d JOIN dbo.EXAM_CYCLE ec ON ec.exam_cycle_id=d.exam_cycle_id
JOIN dbo.ACADEMIC_SESSION a ON a.session_id=ec.session_id CROSS JOIN dbo.SYSTEM_SETTINGS ss;
GO

-- MODULE: 02_Core_Procedures.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Internal authorization helper. ActorId must come from the authenticated API identity.
CREATE OR ALTER PROCEDURE dbo.usp_RequireRole
@ActorId int, @RoleCode nvarchar(40), @DepartmentId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId AND role_code=@RoleCode
 AND (@DepartmentId IS NULL OR department_id=@DepartmentId OR department_id IS NULL))
 THROW 51001, 'This action is not allowed for your active role/department.', 1;
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_CanAccessExam(@ActorId int,@ExamId int)
RETURNS bit AS
BEGIN
 DECLARE @Allowed bit=0;
 SELECT @Allowed=1 FROM dbo.vw_CourseExam ce WHERE ce.course_exam_id=@ExamId
 AND (EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole r WHERE r.user_id=@ActorId AND
 (r.role_code IN(N'DIRECTOR',N'DATA_CELL') OR (r.role_code=N'HOD' AND (r.department_id IS NULL OR r.department_id=ce.lead_department_id))))
 OR EXISTS(SELECT 1 FROM dbo.TEACHING_ASSIGNMENT ta JOIN dbo.OFFERING_SECTION os ON os.offering_section_id=ta.offering_section_id JOIN dbo.vw_ActiveUserRole r ON r.user_id=ta.teacher_user_id AND r.role_code=N'TEACHER'
 WHERE os.offering_id=ce.offering_id AND ta.teacher_user_id=@ActorId AND ta.starts_at<=SYSUTCDATETIME())
 OR EXISTS(SELECT 1 FROM dbo.DIRECTOR_DELEGATION dd JOIN dbo.vw_ActiveUserRole gr ON gr.user_role_id=dd.grantor_user_role_id AND gr.role_code=N'DIRECTOR'
 JOIN dbo.APP_USER u ON u.user_id=dd.delegate_user_id AND u.is_active=1
 WHERE dd.delegate_user_id=@ActorId AND dd.revoked_at IS NULL AND dd.starts_at<=SYSUTCDATETIME() AND dd.ends_at>SYSUTCDATETIME()));
 RETURN @Allowed;
END;
GO
-- Internal helper: verifies the actor can see this course/exam.
CREATE OR ALTER PROCEDURE dbo.usp_RequireExamAccess
@ActorId int, @ExamId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF dbo.fn_CanAccessExam(@ActorId,@ExamId)=0 THROW 51002, 'Course/exam access denied.', 1;
END;
GO

-- Internal helper: locks a draft and checks setter ownership plus optimistic concurrency.
CREATE OR ALTER PROCEDURE dbo.usp_RequireDraftSetter
@ActorId int, @VersionId int, @ExpectedRowVersion binary(8)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 DECLARE @PaperId int,@Frozen datetime2(0),@Actual binary(8);
SELECT @PaperId=paper_id,@Frozen=frozen_at,@Actual=row_version FROM dbo.PAPER_VERSION WITH(UPDLOCK,HOLDLOCK) WHERE paper_version_id=@VersionId;
IF @PaperId IS NULL OR @Frozen IS NOT NULL THROW 51003, 'Version missing or frozen; create a new revision.', 1;
IF @ExpectedRowVersion IS NULL OR @Actual<>@ExpectedRowVersion THROW 51004, 'Workspace changed. Reload and retry.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER_ASSIGNMENT WHERE paper_id=@PaperId AND setter_user_id=@ActorId AND ended_at IS NULL)
 THROW 51005, 'Only the assigned setter can finalize or reorganize the draft.', 1;
EXEC dbo.usp_RequireRole @ActorId,N'TEACHER';
IF EXISTS(SELECT 1 FROM dbo.PAPER_VERSION WHERE paper_id=@PaperId AND paper_version_id>@VersionId)
 THROW 51006, 'Only the latest draft may be edited.', 1;
END;
GO

-- Returns the global default session/exam; NULL IDs mean Data Cell has not published a datesheet.
CREATE OR ALTER PROCEDURE dbo.usp_GetCurrentExam
@ActorId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId) THROW 51001,'Active role required.',1;
SELECT * FROM dbo.vw_CurrentExam;
END;
GO

-- Returns the signed-in user profile and active roles; never returns password_hash.
CREATE OR ALTER PROCEDURE dbo.usp_GetProfile
@ActorId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 SELECT user_id,staff_number,full_name,email,phone_number,designation,profile_photo_file_asset_id,home_department_id,is_active FROM dbo.APP_USER WHERE user_id=@ActorId AND is_active=1;
SELECT user_role_id,role_code,department_id FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId;
END;
GO

-- Updates personal profile. Email, designation and role changes are administrative operations.
CREATE OR ALTER PROCEDURE dbo.usp_UpdateProfile
@ActorId int,@FullName nvarchar(200),@Phone nvarchar(30)=NULL,@PhotoFileId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
IF NULLIF(LTRIM(RTRIM(@FullName)),N'') IS NULL THROW 51010,'Name is required.',1;
IF @PhotoFileId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.FILE_ASSET WHERE file_asset_id=@PhotoFileId AND uploaded_by_user_id=@ActorId AND mime_type LIKE N'image/%') THROW 51010,'Invalid profile image.',1;
UPDATE dbo.APP_USER SET full_name=@FullName,phone_number=@Phone,profile_photo_file_asset_id=@PhotoFileId WHERE user_id=@ActorId AND is_active=1;
IF @@ROWCOUNT=0 THROW 51010,'Active user not found.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Trusted authentication service ONLY: verify password in ASP.NET; never expose this result to the client.
CREATE OR ALTER PROCEDURE dbo.usp_GetLoginRecord
@Email nvarchar(256)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 SELECT user_id,password_hash,is_active FROM dbo.APP_USER WHERE email=@Email;
END;
GO

-- API must verify current password/reset token before calling; SQL never hashes plaintext passwords.
CREATE OR ALTER PROCEDURE dbo.usp_ChangePasswordHash
@ActorId int,@NewPasswordHash nvarchar(256)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
IF LEN(@NewPasswordHash)<30 THROW 51010,'Pass a password hash produced by the API password hasher.',1;
UPDATE dbo.APP_USER SET password_hash=@NewPasswordHash WHERE user_id=@ActorId AND is_active=1;
IF @@ROWCOUNT=0 THROW 51010,'Active user not found.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Records a file already uploaded/validated by the API; bytes stay in file storage.
CREATE OR ALTER PROCEDURE dbo.usp_RegisterFile
@ActorId int,@StorageKey nvarchar(256),@OriginalName nvarchar(256),@MimeType nvarchar(128),@Checksum nvarchar(256)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
IF NOT EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId) THROW 51001,'Active user role required.',1;
INSERT dbo.FILE_ASSET(uploaded_by_user_id,storage_key,original_name,mime_type,checksum,created_at)
OUTPUT inserted.file_asset_id VALUES(@ActorId,@StorageKey,@OriginalName,@MimeType,@Checksum,SYSUTCDATETIME());
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO


-- MODULE: 03_Academic_Setup.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Data Cell creates department. Duplicate natural keys are rejected by table constraints.
CREATE OR ALTER PROCEDURE dbo.usp_AddDepartment
@ActorId int,@department_code nvarchar(40),@department_name nvarchar(200)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
INSERT dbo.DEPARTMENT(department_code,department_name) OUTPUT inserted.department_id VALUES(@department_code,@department_name);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell creates program. Duplicate natural keys are rejected by table constraints.
CREATE OR ALTER PROCEDURE dbo.usp_AddProgram
@ActorId int,@department_id int,@program_code nvarchar(40),@program_name nvarchar(200)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
INSERT dbo.PROGRAM(department_id,program_code,program_name) OUTPUT inserted.program_id VALUES(@department_id,@program_code,@program_name);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell creates session. Duplicate natural keys are rejected by table constraints.
CREATE OR ALTER PROCEDURE dbo.usp_AddSession
@ActorId int,@session_name nvarchar(200),@starts_on date,@ends_on date
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
INSERT dbo.ACADEMIC_SESSION(session_name,starts_on,ends_on) OUTPUT inserted.session_id VALUES(@session_name,@starts_on,@ends_on);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell creates section. Duplicate natural keys are rejected by table constraints.
CREATE OR ALTER PROCEDURE dbo.usp_AddSection
@ActorId int,@program_id int,@session_id int,@section_name nvarchar(200),@semester_number int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
INSERT dbo.SECTION(program_id,session_id,section_name,semester_number) OUTPUT inserted.section_id VALUES(@program_id,@session_id,@section_name,@semester_number);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell creates course. Duplicate natural keys are rejected by table constraints.
CREATE OR ALTER PROCEDURE dbo.usp_AddCourse
@ActorId int,@owner_department_id int,@course_code nvarchar(40),@course_title nvarchar(200)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
INSERT dbo.COURSE(owner_department_id,course_code,course_title) OUTPUT inserted.course_id VALUES(@owner_department_id,@course_code,@course_title);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell creates examcycle. Duplicate natural keys are rejected by table constraints.
CREATE OR ALTER PROCEDURE dbo.usp_AddExamCycle
@ActorId int,@session_id int,@exam_type nvarchar(40)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
INSERT dbo.EXAM_CYCLE(session_id,exam_type) OUTPUT inserted.exam_cycle_id VALUES(@session_id,@exam_type);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell imports a staff account. Role assignment is a separate Director-authorized operation.
CREATE OR ALTER PROCEDURE dbo.usp_CreateUser
@ActorId int,@StaffNumber nvarchar(200),@FullName nvarchar(200),@Email nvarchar(256),@PasswordHash nvarchar(256),@DepartmentId int,@Designation nvarchar(100)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
IF LEN(@PasswordHash)<30 THROW 51010,'API-generated password hash is required.',1;
INSERT dbo.APP_USER(staff_number,full_name,email,password_hash,home_department_id,designation)
OUTPUT inserted.user_id VALUES(@StaffNumber,@FullName,@Email,@PasswordHash,@DepartmentId,@Designation);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Director grants a staff role. Bootstrap first Director manually once; there is no Student role.
CREATE OR ALTER PROCEDURE dbo.usp_GrantUserRole
@ActorId int,@UserId int,@RoleCode nvarchar(40),@DepartmentId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DIRECTOR';
DECLARE @RoleId int=(SELECT role_id FROM dbo.ROLE WHERE role_code=@RoleCode);
IF @RoleId IS NULL THROW 51010,'Unknown role.',1;
INSERT dbo.USER_ROLE(user_id,role_id,department_id,valid_from) OUTPUT inserted.user_role_id VALUES(@UserId,@RoleId,@DepartmentId,SYSUTCDATETIME());
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Ends another staff role without deleting its history.
CREATE OR ALTER PROCEDURE dbo.usp_EndUserRole
@ActorId int,@UserRoleId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DIRECTOR';
IF EXISTS(SELECT 1 FROM dbo.USER_ROLE WHERE user_role_id=@UserRoleId AND user_id=@ActorId) THROW 51010,'Do not revoke your own role through this action.',1;
UPDATE dbo.USER_ROLE SET valid_until=SYSUTCDATETIME() WHERE user_role_id=@UserRoleId AND valid_until IS NULL AND valid_from<SYSUTCDATETIME();
IF @@ROWCOUNT=0 THROW 51010,'Role not found, already ended or too new to end in the same second.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- HOD creates an unpublished outline. A published outline is treated as immutable.
CREATE OR ALTER PROCEDURE dbo.usp_CreateOutline
@ActorId int,@CourseId int,@VersionNumber int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int=(SELECT owner_department_id FROM dbo.COURSE WHERE course_id=@CourseId);
IF @Dept IS NULL THROW 51010,'Course not found.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
INSERT dbo.COURSE_OUTLINE(course_id,version_number) OUTPUT inserted.outline_id VALUES(@CourseId,@VersionNumber);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Adds a topic/subtopic to a draft outline. New child IDs prevent ancestor cycles.
CREATE OR ALTER PROCEDURE dbo.usp_AddOutlineTopic
@ActorId int,@OutlineId int,@Title nvarchar(200),@ParentTopicId int=NULL,@Week int=NULL,@SortOrder int=1
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int;
SELECT @Dept=c.owner_department_id FROM dbo.COURSE_OUTLINE o WITH(UPDLOCK,HOLDLOCK) JOIN dbo.COURSE c ON c.course_id=o.course_id WHERE o.outline_id=@OutlineId AND o.published_at IS NULL;
IF @Dept IS NULL THROW 51010,'Outline missing or already published.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
INSERT dbo.TOPIC(outline_id,topic_title,parent_topic_id,week_number,sort_order) OUTPUT inserted.topic_id VALUES(@OutlineId,@Title,@ParentTopicId,@Week,@SortOrder);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Adds a course-specific CLO statement; no CLO marks/weight fields.
CREATE OR ALTER PROCEDURE dbo.usp_AddOutlineCLO
@ActorId int,@OutlineId int,@Code nvarchar(40),@Statement nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int;
SELECT @Dept=c.owner_department_id FROM dbo.COURSE_OUTLINE o WITH(UPDLOCK,HOLDLOCK) JOIN dbo.COURSE c ON c.course_id=o.course_id WHERE o.outline_id=@OutlineId AND o.published_at IS NULL;
IF @Dept IS NULL THROW 51010,'Outline missing or published.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
INSERT dbo.CLO(outline_id,clo_code,clo_statement) OUTPUT inserted.clo_id VALUES(@OutlineId,@Code,@Statement);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Publishes an outline; later curriculum changes require a new outline version.
CREATE OR ALTER PROCEDURE dbo.usp_PublishOutline
@ActorId int,@OutlineId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int;
SELECT @Dept=c.owner_department_id FROM dbo.COURSE_OUTLINE o WITH(UPDLOCK,HOLDLOCK) JOIN dbo.COURSE c ON c.course_id=o.course_id WHERE o.outline_id=@OutlineId AND o.published_at IS NULL;
IF @Dept IS NULL THROW 51010,'Draft outline not found.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
IF NOT EXISTS(SELECT 1 FROM dbo.TOPIC WHERE outline_id=@OutlineId) OR NOT EXISTS(SELECT 1 FROM dbo.CLO WHERE outline_id=@OutlineId) THROW 51010,'Add topics and CLOs first.',1;
UPDATE dbo.COURSE_OUTLINE SET published_at=SYSUTCDATETIME() WHERE outline_id=@OutlineId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Creates a course offering for a session using a published course outline.
CREATE OR ALTER PROCEDURE dbo.usp_AddCourseOffering
@ActorId int,@CourseId int,@OutlineId int,@SessionId int,@DepartmentId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
IF NOT EXISTS(SELECT 1 FROM dbo.COURSE_OUTLINE WHERE outline_id=@OutlineId AND course_id=@CourseId AND published_at IS NOT NULL) THROW 51010,'Use a published outline for this course.',1;
INSERT dbo.COURSE_OFFERING(course_id,outline_id,session_id,lead_department_id) OUTPUT inserted.offering_id VALUES(@CourseId,@OutlineId,@SessionId,@DepartmentId);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Connects a section to its course; cannot expand teaching scope after common topics are locked.
CREATE OR ALTER PROCEDURE dbo.usp_AddOfferingSection
@ActorId int,@OfferingId int,@SectionId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
IF NOT EXISTS(SELECT 1 FROM dbo.COURSE_OFFERING o JOIN dbo.SECTION s ON s.session_id=o.session_id WHERE o.offering_id=@OfferingId AND s.section_id=@SectionId) THROW 51010,'Section and offering must share the session.',1;
IF EXISTS(SELECT 1 FROM dbo.COURSE_EXAM WITH(UPDLOCK,HOLDLOCK) WHERE offering_id=@OfferingId AND scope_locked_at IS NOT NULL) THROW 51010,'Teaching scope is locked for this offering.',1;
INSERT dbo.OFFERING_SECTION(offering_id,section_id) OUTPUT inserted.offering_section_id VALUES(@OfferingId,@SectionId);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell records which teacher teaches which section. Paper setter assignment remains HOD-only.
CREATE OR ALTER PROCEDURE dbo.usp_AssignTeaching
@ActorId int,@OfferingSectionId int,@TeacherId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
EXEC dbo.usp_RequireRole @TeacherId,N'TEACHER';
IF EXISTS(SELECT 1 FROM dbo.COURSE_EXAM ce WITH(UPDLOCK,HOLDLOCK) JOIN dbo.OFFERING_SECTION os ON os.offering_id=ce.offering_id WHERE os.offering_section_id=@OfferingSectionId AND ce.scope_locked_at IS NOT NULL) THROW 51010,'Teaching scope is locked.',1;
INSERT dbo.TEACHING_ASSIGNMENT(offering_section_id,teacher_user_id,starts_at) OUTPUT inserted.teaching_assignment_id VALUES(@OfferingSectionId,@TeacherId,SYSUTCDATETIME());
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- HOD defines course exam marks/duration; these are snapshotted into each paper version.
CREATE OR ALTER PROCEDURE dbo.usp_ConfigureCourseExam
@ActorId int,@OfferingId int,@ExamCycleId int,@Marks decimal(10,2),@Duration int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int=(SELECT lead_department_id FROM dbo.COURSE_OFFERING WHERE offering_id=@OfferingId);
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
IF NOT EXISTS(SELECT 1 FROM dbo.COURSE_OFFERING o JOIN dbo.EXAM_CYCLE ec ON ec.session_id=o.session_id WHERE o.offering_id=@OfferingId AND ec.exam_cycle_id=@ExamCycleId) THROW 51010,'Offering/exam session mismatch.',1;
INSERT dbo.COURSE_EXAM(offering_id,exam_cycle_id,required_marks,duration_minutes,configured_by_user_id)
OUTPUT inserted.course_exam_id VALUES(@OfferingId,@ExamCycleId,@Marks,@Duration,@ActorId);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Sets eligible CLOs per course exam, e.g. [1,2,3]. CLO-1 of another course is not interchangeable.
CREATE OR ALTER PROCEDURE dbo.usp_SetExamCLOs
@ActorId int,@ExamId int,@CloIdsJson nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int,@Outline int,@Locked datetime2(0);
SELECT @Dept=o.lead_department_id,@Outline=o.outline_id,@Locked=ce.scope_locked_at FROM dbo.COURSE_EXAM ce WITH(UPDLOCK,HOLDLOCK) JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id WHERE ce.course_exam_id=@ExamId;
IF @Outline IS NULL OR @Locked IS NOT NULL THROW 51010,'Exam missing or scope locked.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
IF ISJSON(@CloIdsJson)<>1 THROW 51010,'CLO IDs must be a JSON array.',1;
DECLARE @Ids TABLE(id int PRIMARY KEY);
INSERT @Ids SELECT CONVERT(int,value) FROM OPENJSON(@CloIdsJson);
IF NOT EXISTS(SELECT 1 FROM @Ids) OR EXISTS(SELECT 1 FROM @Ids i WHERE NOT EXISTS(SELECT 1 FROM dbo.CLO WHERE clo_id=i.id AND outline_id=@Outline)) THROW 51010,'Choose CLOs belonging to this course outline.',1;
DELETE dbo.EXAM_CLO WHERE course_exam_id=@ExamId AND clo_id NOT IN(SELECT id FROM @Ids);
INSERT dbo.EXAM_CLO(course_exam_id,clo_id) SELECT @ExamId,id FROM @Ids i WHERE NOT EXISTS(SELECT 1 FROM dbo.EXAM_CLO WHERE course_exam_id=@ExamId AND clo_id=i.id);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO


-- MODULE: 04_Coverage_And_Datesheet.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Teacher saves the complete selection for ONE assigned section; [] with Submit=1 means explicitly no topics covered.
CREATE OR ALTER PROCEDURE dbo.usp_SaveCoverage
@ActorId int,@ExamId int,@TeachingAssignmentId int,@TopicsJson nvarchar(max),@Submit bit=0
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Locked datetime2(0),@Outline int;
SELECT @Locked=ce.scope_locked_at,@Outline=o.outline_id FROM dbo.COURSE_EXAM ce WITH(UPDLOCK,HOLDLOCK) JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id WHERE ce.course_exam_id=@ExamId;
IF @Outline IS NULL OR @Locked IS NOT NULL THROW 51020,'Exam missing or topic scope already locked.',1;
EXEC dbo.usp_RequireRole @ActorId,N'TEACHER';
IF NOT EXISTS(SELECT 1 FROM dbo.vw_RequiredCoverage WHERE course_exam_id=@ExamId AND teaching_assignment_id=@TeachingAssignmentId AND teacher_user_id=@ActorId) THROW 51020,'This section is not assigned to you.',1;
IF ISJSON(@TopicsJson)<>1 THROW 51020,'Supply topics as JSON.',1;
DECLARE @Topics TABLE(topic_id int PRIMARY KEY,status nvarchar(40) NOT NULL);
INSERT @Topics SELECT topic_id,status FROM OPENJSON(@TopicsJson) WITH(topic_id int,status nvarchar(40));
IF EXISTS(SELECT 1 FROM @Topics t WHERE status NOT IN(N'COVERED',N'IN_PROGRESS',N'NOT_STARTED') OR NOT EXISTS(SELECT 1 FROM dbo.TOPIC WHERE topic_id=t.topic_id AND outline_id=@Outline)) THROW 51020,'Invalid topic/status for this course.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.TOPIC_COVERAGE_SUBMISSION WHERE teaching_assignment_id=@TeachingAssignmentId AND course_exam_id=@ExamId)
 INSERT dbo.TOPIC_COVERAGE_SUBMISSION(teaching_assignment_id,course_exam_id) VALUES(@TeachingAssignmentId,@ExamId);
UPDATE dbo.TOPIC_COVERAGE_SUBMISSION SET submission_status=CASE WHEN @Submit=1 THEN N'SUBMITTED' ELSE N'DRAFT' END,
 submitted_by_user_id=CASE WHEN @Submit=1 THEN @ActorId END,submitted_at=CASE WHEN @Submit=1 THEN SYSUTCDATETIME() END
WHERE teaching_assignment_id=@TeachingAssignmentId AND course_exam_id=@ExamId;
DELETE dbo.TOPIC_COVERAGE WHERE teaching_assignment_id=@TeachingAssignmentId AND course_exam_id=@ExamId;
INSERT dbo.TOPIC_COVERAGE(teaching_assignment_id,course_exam_id,topic_id,coverage_status) SELECT @TeachingAssignmentId,@ExamId,topic_id,status FROM @Topics;
SELECT * FROM dbo.vw_RequiredCoverage WHERE teaching_assignment_id=@TeachingAssignmentId AND course_exam_id=@ExamId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Four result sets: teacher/section submissions, topic matrix, common topics, eligible CLOs.
CREATE OR ALTER PROCEDURE dbo.usp_GetTopicCoverage
@ActorId int,@ExamId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 EXEC dbo.usp_RequireExamAccess @ActorId,@ExamId;
SELECT * FROM dbo.vw_RequiredCoverage WHERE course_exam_id=@ExamId;
SELECT t.topic_id,t.parent_topic_id,t.topic_title,t.sort_order,r.teaching_assignment_id,r.section_name,r.full_name,
 COALESCE(tc.coverage_status,N'NOT_STARTED') AS coverage_status,r.submission_status
FROM dbo.vw_CourseExam ce JOIN dbo.TOPIC t ON t.outline_id=ce.outline_id JOIN dbo.vw_RequiredCoverage r ON r.course_exam_id=ce.course_exam_id
LEFT JOIN dbo.TOPIC_COVERAGE tc ON tc.course_exam_id=ce.course_exam_id AND tc.teaching_assignment_id=r.teaching_assignment_id AND tc.topic_id=t.topic_id
WHERE ce.course_exam_id=@ExamId ORDER BY t.sort_order,t.topic_id,r.section_name;
SELECT * FROM dbo.vw_ComputedCommonTopic WHERE course_exam_id=@ExamId;
SELECT ec.exam_clo_id,c.clo_code,c.clo_statement FROM dbo.EXAM_CLO ec JOIN dbo.CLO c ON c.clo_id=ec.clo_id WHERE ec.course_exam_id=@ExamId;
END;
GO

-- HOD locks the common-topic intersection after every required section submits. Coverage cannot then change silently.
CREATE OR ALTER PROCEDURE dbo.usp_LockCommonTopics
@ActorId int,@ExamId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int,@Locked datetime2(0);
SELECT @Dept=o.lead_department_id,@Locked=ce.scope_locked_at FROM dbo.COURSE_EXAM ce WITH(UPDLOCK,HOLDLOCK) JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id WHERE ce.course_exam_id=@ExamId;
IF @Dept IS NULL OR @Locked IS NOT NULL THROW 51020,'Exam missing or already locked.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_ComputedCommonTopic WHERE course_exam_id=@ExamId) THROW 51020,'All sections must submit and have at least one common topic.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.EXAM_CLO WHERE course_exam_id=@ExamId) THROW 51020,'Configure exam CLOs first.',1;
INSERT dbo.EXAM_COMMON_TOPIC(course_exam_id,topic_id) SELECT course_exam_id,topic_id FROM dbo.vw_ComputedCommonTopic WHERE course_exam_id=@ExamId;
UPDATE dbo.COURSE_EXAM SET scope_locked_at=SYSUTCDATETIME() WHERE course_exam_id=@ExamId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Creates a validated preview revision. Does NOT change current exam. Correct a preview by creating a new draft.
CREATE OR ALTER PROCEDURE dbo.usp_SaveDatesheetDraft
@ActorId int,@ExamCycleId int,@ScheduleJson nvarchar(max),@ImportBatchId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
DECLARE @Revision int,@DatesheetId int,@Cycle int;
SELECT @Cycle=exam_cycle_id FROM dbo.EXAM_CYCLE WITH(UPDLOCK,HOLDLOCK) WHERE exam_cycle_id=@ExamCycleId;
IF @Cycle IS NULL THROW 51021,'Exam cycle does not exist.',1;
IF ISJSON(@ScheduleJson)<>1 THROW 51021,'Schedule JSON is invalid.',1;
DECLARE @Schedule TABLE(course_exam_id int PRIMARY KEY,exam_date date NOT NULL,start_time time(0) NOT NULL,end_time time(0) NOT NULL);
INSERT @Schedule SELECT course_exam_id,exam_date,start_time,end_time FROM OPENJSON(@ScheduleJson) WITH(course_exam_id int,exam_date date,start_time time(0),end_time time(0));
IF NOT EXISTS(SELECT 1 FROM @Schedule) OR EXISTS(SELECT 1 FROM @Schedule s WHERE end_time<=start_time OR NOT EXISTS(SELECT 1 FROM dbo.COURSE_EXAM WHERE course_exam_id=s.course_exam_id AND exam_cycle_id=@ExamCycleId)) THROW 51021,'Empty schedule, invalid times or course/exam mismatch.',1;
SELECT @Revision=COALESCE(MAX(revision_number),0)+1 FROM dbo.DATESHEET WHERE exam_cycle_id=@ExamCycleId;
INSERT dbo.DATESHEET(exam_cycle_id,import_batch_id,revision_number) VALUES(@ExamCycleId,@ImportBatchId,@Revision);
SET @DatesheetId=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.EXAM_SCHEDULE(datesheet_id,course_exam_id,exam_cycle_id,exam_date,start_time,end_time)
SELECT @DatesheetId,course_exam_id,@ExamCycleId,exam_date,start_time,end_time FROM @Schedule;
SELECT @DatesheetId AS datesheet_id,@Revision AS revision_number,N'DRAFT' AS status;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Returns preview header, schedule rows and missing-course errors before confirmation.
CREATE OR ALTER PROCEDURE dbo.usp_GetDatesheetPreview
@ActorId int,@DatesheetId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
SELECT * FROM dbo.vw_DatesheetHistory WHERE datesheet_id=@DatesheetId;
SELECT s.schedule_id,ce.course_code,ce.course_title,s.exam_date,s.start_time,s.end_time FROM dbo.EXAM_SCHEDULE s JOIN dbo.vw_CourseExam ce ON ce.course_exam_id=s.course_exam_id WHERE s.datesheet_id=@DatesheetId ORDER BY s.exam_date,s.start_time,ce.course_code;
SELECT ce.course_exam_id,ce.course_code,N'MISSING_SCHEDULE' AS validation_error FROM dbo.vw_CourseExam ce JOIN dbo.DATESHEET d ON d.exam_cycle_id=ce.exam_cycle_id WHERE d.datesheet_id=@DatesheetId AND NOT EXISTS(SELECT 1 FROM dbo.EXAM_SCHEDULE s WHERE s.datesheet_id=d.datesheet_id AND s.course_exam_id=ce.course_exam_id);
END;
GO

-- Confirm & Set Current: publishes, supersedes the old same-cycle revision, updates defaults and audits atomically.
CREATE OR ALTER PROCEDURE dbo.usp_PublishDatesheet
@ActorId int,@DatesheetId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
DECLARE @Cycle int,@Previous int,@Status nvarchar(20);
SELECT @Previous=active_datesheet_id FROM dbo.SYSTEM_SETTINGS WITH(UPDLOCK,HOLDLOCK) WHERE settings_id=1;
SELECT @Cycle=exam_cycle_id,@Status=status FROM dbo.DATESHEET WITH(UPDLOCK,HOLDLOCK) WHERE datesheet_id=@DatesheetId;
IF @Cycle IS NULL OR @Status<>N'DRAFT' THROW 51021,'Select an unpublished draft.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.EXAM_SCHEDULE WHERE datesheet_id=@DatesheetId) OR EXISTS(SELECT 1 FROM dbo.COURSE_EXAM ce WHERE ce.exam_cycle_id=@Cycle AND NOT EXISTS(SELECT 1 FROM dbo.EXAM_SCHEDULE s WHERE s.datesheet_id=@DatesheetId AND s.course_exam_id=ce.course_exam_id)) THROW 51021,'All configured courses must have valid schedule entries.',1;
UPDATE dbo.DATESHEET SET status=N'SUPERSEDED' WHERE exam_cycle_id=@Cycle AND status=N'PUBLISHED';
UPDATE dbo.DATESHEET SET status=N'PUBLISHED',published_by_user_id=@ActorId,published_at=SYSUTCDATETIME() WHERE datesheet_id=@DatesheetId;
UPDATE dbo.SYSTEM_SETTINGS SET active_exam_cycle_id=@Cycle,active_datesheet_id=@DatesheetId,updated_by_user_id=@ActorId,updated_at=SYSUTCDATETIME() WHERE settings_id=1;
INSERT dbo.AUDIT_EVENT(actor_user_id,entity_type,entity_key,action,after_data,occurred_at)
VALUES(@ActorId,N'DATESHEET',CONVERT(nvarchar(200),@DatesheetId),N'PUBLISH_AND_SET_CURRENT',(SELECT @Cycle AS exam_cycle_id,@Previous AS previous_datesheet_id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME());
SELECT * FROM dbo.vw_CurrentExam;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell sees draft, published, superseded revisions and the global current marker.
CREATE OR ALTER PROCEDURE dbo.usp_GetDatesheetHistory
@ActorId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
SELECT * FROM dbo.vw_DatesheetHistory ORDER BY datesheet_id DESC;
END;
GO


-- MODULE: 05_Paper_Workspace.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- HOD assigns/reassigns one setter. Other course teachers are registered as contributors.
CREATE OR ALTER PROCEDURE dbo.usp_AssignPaperSetter
@ActorId int,@ExamId int,@TeacherId int,@DueAt datetime2(0)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int,@PaperId int,@Locked datetime2(0);
SELECT @Dept=o.lead_department_id,@Locked=ce.scope_locked_at FROM dbo.COURSE_EXAM ce WITH(UPDLOCK,HOLDLOCK) JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id WHERE ce.course_exam_id=@ExamId;
IF @Dept IS NULL THROW 51030,'Exam missing.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
EXEC dbo.usp_RequireRole @TeacherId,N'TEACHER';
IF NOT EXISTS(SELECT 1 FROM dbo.vw_RequiredCoverage WHERE course_exam_id=@ExamId AND teacher_user_id=@TeacherId) THROW 51030,'Setter must teach this course.',1;
SELECT @PaperId=paper_id FROM dbo.PAPER WITH(UPDLOCK,HOLDLOCK) WHERE course_exam_id=@ExamId;
IF @PaperId IS NULL BEGIN
 INSERT dbo.PAPER(course_exam_id,workflow_status) VALUES(@ExamId,N'ASSIGNED'); SET @PaperId=CONVERT(int,SCOPE_IDENTITY());
END;
IF EXISTS(SELECT 1 FROM dbo.PAPER WHERE paper_id=@PaperId AND workflow_status IN(N'SUBMITTED',N'HOD_REVIEWED',N'APPROVED',N'RECEIVED',N'ARCHIVED')) THROW 51030,'Cannot reassign a paper in review or finalized.',1;
UPDATE dbo.PAPER_ASSIGNMENT SET ended_at=SYSUTCDATETIME() WHERE paper_id=@PaperId AND ended_at IS NULL;
DECLARE @RoleId int=(SELECT TOP(1) user_role_id FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId AND role_code=N'HOD' AND (department_id=@Dept OR department_id IS NULL) ORDER BY user_role_id);
INSERT dbo.PAPER_ASSIGNMENT(paper_id,setter_user_id,assigned_by_user_role_id,assigned_at,due_at) VALUES(@PaperId,@TeacherId,@RoleId,SYSUTCDATETIME(),@DueAt);
INSERT dbo.PAPER_PARTICIPANT(paper_id,teacher_user_id,joined_at)
SELECT DISTINCT @PaperId,r.teacher_user_id,SYSUTCDATETIME() FROM dbo.vw_RequiredCoverage r WHERE r.course_exam_id=@ExamId AND NOT EXISTS(SELECT 1 FROM dbo.PAPER_PARTICIPANT pp WHERE pp.paper_id=@PaperId AND pp.teacher_user_id=r.teacher_user_id);
INSERT dbo.NOTIFICATION(recipient_user_id,paper_id,event_type,message_text,created_at) VALUES(@TeacherId,@PaperId,N'PAPER_ASSIGNED',N'You have been assigned to set the paper.',SYSUTCDATETIME());
SELECT @PaperId AS paper_id;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Setter creates first draft or a correction revision. Existing question versions are reused read-only until replaced.
CREATE OR ALTER PROCEDURE dbo.usp_StartPaperRevision
@ActorId int,@PaperId int,@Reason nvarchar(500)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @ExamId int,@Status nvarchar(40),@Previous int,@Revision int,@New int;
SELECT @ExamId=course_exam_id,@Status=workflow_status FROM dbo.PAPER WITH(UPDLOCK,HOLDLOCK) WHERE paper_id=@PaperId AND deleted_at IS NULL;
IF @ExamId IS NULL OR @Status NOT IN(N'ASSIGNED',N'DRAFT',N'CHANGES_REQUESTED') THROW 51030,'Paper cannot start a revision in this state.',1;
EXEC dbo.usp_RequireRole @ActorId,N'TEACHER';
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER_ASSIGNMENT WHERE paper_id=@PaperId AND setter_user_id=@ActorId AND ended_at IS NULL) THROW 51030,'Only the setter can start a revision.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.COURSE_EXAM WHERE course_exam_id=@ExamId AND scope_locked_at IS NOT NULL) THROW 51030,'HOD must lock common topics first.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_VERSION WHERE paper_id=@PaperId AND frozen_at IS NULL) THROW 51030,'An editable draft already exists; open it.',1;
SELECT TOP(1) @Previous=paper_version_id,@Revision=revision_number FROM dbo.PAPER_VERSION WHERE paper_id=@PaperId ORDER BY revision_number DESC;
INSERT dbo.PAPER_VERSION(paper_id,revision_number,based_on_version_id,created_by_user_id,created_at,revision_reason,instructions,content_hash,required_marks_snapshot,duration_minutes_snapshot)
SELECT @PaperId,COALESCE(@Revision,0)+1,@Previous,@ActorId,SYSUTCDATETIME(),@Reason,
 (SELECT instructions FROM dbo.PAPER_VERSION WHERE paper_version_id=@Previous),N'DRAFT',required_marks,duration_minutes FROM dbo.COURSE_EXAM WHERE course_exam_id=@ExamId;
SET @New=CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.PAPER_ITEM(paper_version_id,question_version_id,placement,sort_order,display_number)
SELECT @New,question_version_id,placement,sort_order,display_number FROM dbo.PAPER_ITEM WHERE paper_version_id=@Previous;
UPDATE dbo.PAPER SET workflow_status=N'DRAFT' WHERE paper_id=@PaperId;
SELECT * FROM dbo.vw_PaperVersionSummary WHERE paper_version_id=@New;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Saves a whole question atomically with parts/subparts and multiple CLOs. Each edit creates a new question revision; prior CLO checks do not carry over.
CREATE OR ALTER PROCEDURE dbo.usp_SaveQuestion
@ActorId int,@VersionId int,@ExpectedRowVersion binary(8),@Prompt nvarchar(max),@Difficulty nvarchar(40),
@PartsJson nvarchar(max),@CloMappingsJson nvarchar(max),@PaperItemId int=NULL,@Placement nvarchar(40)=N'EXTRA'
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @PaperId int,@ExamId int,@Actual binary(8),@Frozen datetime2(0),@QId int,@OldQV int,@NewQV int,@Revision int,@IsSetter bit=0;
SELECT @PaperId=v.paper_id,@ExamId=p.course_exam_id,@Actual=v.row_version,@Frozen=v.frozen_at FROM dbo.PAPER_VERSION v WITH(UPDLOCK,HOLDLOCK) JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE v.paper_version_id=@VersionId;
IF @PaperId IS NULL OR @Frozen IS NOT NULL OR @ExpectedRowVersion IS NULL OR @Actual<>@ExpectedRowVersion THROW 51031,'Draft missing, frozen or stale. Reload.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_VERSION WHERE paper_id=@PaperId AND revision_number>(SELECT revision_number FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId)) THROW 51031,'Only latest draft is editable.',1;
EXEC dbo.usp_RequireRole @ActorId,N'TEACHER';
EXEC dbo.usp_RequireExamAccess @ActorId,@ExamId;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER_PARTICIPANT WHERE paper_id=@PaperId AND teacher_user_id=@ActorId AND left_at IS NULL) THROW 51031,'Not a paper contributor.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ASSIGNMENT WHERE paper_id=@PaperId AND setter_user_id=@ActorId AND ended_at IS NULL) SET @IsSetter=1;
IF @IsSetter=0 AND @Placement<>N'EXTRA' THROW 51031,'Contributors add extra questions; setter selects MAIN questions.',1;
IF @PaperItemId IS NOT NULL BEGIN
 SELECT @QId=qv.question_id,@OldQV=qv.question_version_id FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_VERSION qv ON qv.question_version_id=i.question_version_id JOIN dbo.QUESTION q ON q.question_id=qv.question_id
 WHERE i.paper_item_id=@PaperItemId AND i.paper_version_id=@VersionId AND (@IsSetter=1 OR (q.author_user_id=@ActorId AND i.placement=N'EXTRA'));
 IF @QId IS NULL THROW 51031,'Question not found or cannot be edited by you.',1;
END;
IF NULLIF(LTRIM(RTRIM(@Prompt)),N'') IS NULL OR ISJSON(@PartsJson)<>1 OR ISJSON(@CloMappingsJson)<>1 THROW 51031,'Prompt, parts JSON and mappings JSON are required.',1;
DECLARE @Parts TABLE(k int PRIMARY KEY,parent_k int NULL,kind nvarchar(10) NOT NULL,label nvarchar(200) NOT NULL,body nvarchar(max) NOT NULL,topic int NULL,marks decimal(10,2) NULL,difficulty nvarchar(40) NULL);
INSERT @Parts SELECT k,parent_k,kind,label,body,topic,marks,difficulty FROM OPENJSON(@PartsJson) WITH(k int,parent_k int,kind nvarchar(10),label nvarchar(200),body nvarchar(max),topic int,marks decimal(10,2),difficulty nvarchar(40));
IF NOT EXISTS(SELECT 1 FROM @Parts WHERE kind=N'ITEM') THROW 51031,'At least one mark-bearing ITEM is required.',1;
IF EXISTS(SELECT 1 FROM @Parts p WHERE k<=0 OR (parent_k IS NOT NULL AND (parent_k>=k OR NOT EXISTS(SELECT 1 FROM @Parts parent WHERE parent.k=p.parent_k AND parent.kind=N'GROUP')))) THROW 51031,'Part keys must be positive; parent GROUP key must precede its child.',1;
IF EXISTS(SELECT 1 FROM @Parts p WHERE kind=N'GROUP' AND NOT EXISTS(SELECT 1 FROM @Parts ch WHERE ch.parent_k=p.k)) THROW 51031,'A GROUP needs children.',1;
IF EXISTS(SELECT 1 FROM @Parts p WHERE kind=N'ITEM' AND NOT EXISTS(SELECT 1 FROM dbo.EXAM_COMMON_TOPIC WHERE course_exam_id=@ExamId AND topic_id=p.topic)) THROW 51031,'Questions must use common topics only.',1;
DECLARE @Maps TABLE(k int NULL,exam_clo_id int NOT NULL);
INSERT @Maps SELECT k,exam_clo_id FROM OPENJSON(@CloMappingsJson) WITH(k int,exam_clo_id int);
IF EXISTS(SELECT 1 FROM @Maps m WHERE NOT EXISTS(SELECT 1 FROM dbo.EXAM_CLO WHERE exam_clo_id=m.exam_clo_id AND course_exam_id=@ExamId) OR (k IS NOT NULL AND NOT EXISTS(SELECT 1 FROM @Parts WHERE k=m.k))) THROW 51031,'Invalid part or CLO mapping.',1;
IF @QId IS NULL BEGIN
 INSERT dbo.QUESTION(paper_id,author_user_id,created_at) VALUES(@PaperId,@ActorId,SYSUTCDATETIME()); SET @QId=CONVERT(int,SCOPE_IDENTITY());
END;
SELECT @Revision=COALESCE(MAX(revision_number),0)+1 FROM dbo.QUESTION_VERSION WHERE question_id=@QId;
INSERT dbo.QUESTION_VERSION(question_id,revision_number,prompt_text,difficulty,created_by_user_id,created_at) VALUES(@QId,@Revision,@Prompt,@Difficulty,@ActorId,SYSUTCDATETIME());
SET @NewQV=CONVERT(int,SCOPE_IDENTITY());
-- part_order uses the positive JSON key, making parent links deterministic without cursors.
INSERT dbo.QUESTION_PART(question_version_id,part_kind,difficulty,part_label,part_order,part_text,topic_id,marks)
SELECT @NewQV,kind,difficulty,label,k,body,topic,marks FROM @Parts;
UPDATE child SET parent_part_id=parent.question_part_id FROM dbo.QUESTION_PART child JOIN @Parts p ON p.k=child.part_order
JOIN dbo.QUESTION_PART parent ON parent.question_version_id=@NewQV AND parent.part_order=p.parent_k WHERE child.question_version_id=@NewQV;
INSERT dbo.CLO_MAPPING(question_version_id,question_part_id,exam_clo_id)
SELECT @NewQV,qp.question_part_id,m.exam_clo_id FROM @Maps m LEFT JOIN dbo.QUESTION_PART qp ON qp.question_version_id=@NewQV AND qp.part_order=m.k;
INSERT dbo.QUESTION_ATTACHMENT(question_version_id,question_part_id,file_asset_id,display_order)
SELECT @NewQV,newpart.question_part_id,a.file_asset_id,a.display_order
FROM dbo.QUESTION_ATTACHMENT a LEFT JOIN dbo.QUESTION_PART oldpart ON oldpart.question_part_id=a.question_part_id
LEFT JOIN dbo.QUESTION_PART newpart ON newpart.question_version_id=@NewQV AND newpart.part_order=oldpart.part_order
WHERE a.question_version_id=@OldQV AND (a.question_part_id IS NULL OR newpart.question_part_id IS NOT NULL);
IF @PaperItemId IS NULL BEGIN
 INSERT dbo.PAPER_ITEM(paper_version_id,question_version_id,placement,sort_order)
 SELECT @VersionId,@NewQV,@Placement,COALESCE(MAX(sort_order),0)+1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND placement=@Placement;
 SET @PaperItemId=CONVERT(int,SCOPE_IDENTITY());
END
ELSE UPDATE dbo.PAPER_ITEM SET question_version_id=@NewQV WHERE paper_item_id=@PaperItemId;
UPDATE dbo.PAPER_VERSION SET content_hash=N'DRAFT' WHERE paper_version_id=@VersionId;
SELECT @PaperItemId AS paper_item_id,@NewQV AS question_version_id,row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Setter includes/removes a question from the actual exam by moving it between MAIN and EXTRA.
CREATE OR ALTER PROCEDURE dbo.usp_MovePaperItem
@ActorId int,@VersionId int,@ExpectedRowVersion binary(8),@PaperItemId int,@Placement nvarchar(40)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireDraftSetter @ActorId,@VersionId,@ExpectedRowVersion;
IF @Placement NOT IN(N'MAIN',N'EXTRA') THROW 51031,'Placement must be MAIN or EXTRA.',1;
UPDATE dbo.PAPER_ITEM SET placement=@Placement,sort_order=(SELECT COALESCE(MAX(sort_order),0)+1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND placement=@Placement) WHERE paper_item_id=@PaperItemId AND paper_version_id=@VersionId;
IF @@ROWCOUNT=0 THROW 51031,'Paper item not found.',1;
UPDATE dbo.PAPER_VERSION SET content_hash=N'DRAFT' WHERE paper_version_id=@VersionId;
SELECT row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Drag/drop reorder: submit all tab item IDs in their desired order. Returns new concurrency token.
CREATE OR ALTER PROCEDURE dbo.usp_ReorderPaperItems
@ActorId int,@VersionId int,@ExpectedRowVersion binary(8),@Placement nvarchar(40),@OrderJson nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireDraftSetter @ActorId,@VersionId,@ExpectedRowVersion;
IF ISJSON(@OrderJson)<>1 THROW 51031,'Order must be a JSON ID array.',1;
DECLARE @Order TABLE(id int PRIMARY KEY,n int UNIQUE);
INSERT @Order SELECT CONVERT(int,value),CONVERT(int,[key])+1 FROM OPENJSON(@OrderJson);
IF (SELECT COUNT(*) FROM @Order)<>(SELECT COUNT(*) FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND placement=@Placement)
 OR EXISTS(SELECT 1 FROM @Order x WHERE NOT EXISTS(SELECT 1 FROM dbo.PAPER_ITEM WHERE paper_item_id=x.id AND paper_version_id=@VersionId AND placement=@Placement)) THROW 51031,'Supply every item in this tab exactly once.',1;
DECLARE @Offset int=(SELECT COALESCE(MAX(sort_order),0)+1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND placement=@Placement);
UPDATE dbo.PAPER_ITEM SET sort_order=sort_order+@Offset WHERE paper_version_id=@VersionId AND placement=@Placement;
UPDATE i SET sort_order=x.n,display_number=CONVERT(nvarchar(200),x.n) FROM dbo.PAPER_ITEM i JOIN @Order x ON x.id=i.paper_item_id;
UPDATE dbo.PAPER_VERSION SET content_hash=N'DRAFT' WHERE paper_version_id=@VersionId;
SELECT row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Setter edits paper instructions while draft is editable.
CREATE OR ALTER PROCEDURE dbo.usp_SavePaperInstructions
@ActorId int,@VersionId int,@ExpectedRowVersion binary(8),@Instructions nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireDraftSetter @ActorId,@VersionId,@ExpectedRowVersion;
UPDATE dbo.PAPER_VERSION SET instructions=@Instructions,content_hash=N'DRAFT' WHERE paper_version_id=@VersionId;
SELECT row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Six result sets: header/totals, questions, parts, CLO results, comments, teachers/sections.
CREATE OR ALTER PROCEDURE dbo.usp_GetPaperWorkspace
@ActorId int,@VersionId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 DECLARE @Exam int=(SELECT p.course_exam_id FROM dbo.PAPER_VERSION v JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE v.paper_version_id=@VersionId);
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
SELECT ce.*,v.paper_version_id,v.revision_number,v.instructions,v.frozen_at,v.row_version,ps.added_marks,ps.main_question_count FROM dbo.PAPER_VERSION v JOIN dbo.PAPER p ON p.paper_id=v.paper_id JOIN dbo.vw_CourseExam ce ON ce.course_exam_id=p.course_exam_id JOIN dbo.vw_PaperVersionSummary ps ON ps.paper_version_id=v.paper_version_id WHERE v.paper_version_id=@VersionId;
SELECT i.*,qv.prompt_text,qv.difficulty,q.author_user_id FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_VERSION qv ON qv.question_version_id=i.question_version_id JOIN dbo.QUESTION q ON q.question_id=qv.question_id WHERE i.paper_version_id=@VersionId ORDER BY i.placement,i.sort_order;
SELECT qp.* FROM dbo.QUESTION_PART qp JOIN dbo.PAPER_ITEM i ON i.question_version_id=qp.question_version_id WHERE i.paper_version_id=@VersionId ORDER BY i.sort_order,qp.part_order;
SELECT m.*,c.clo_code,c.clo_statement,ck.check_status,ck.explanation FROM dbo.CLO_MAPPING m JOIN dbo.PAPER_ITEM i ON i.question_version_id=m.question_version_id JOIN dbo.EXAM_CLO ec ON ec.exam_clo_id=m.exam_clo_id JOIN dbo.CLO c ON c.clo_id=ec.clo_id LEFT JOIN dbo.vw_LatestCLOCheck ck ON ck.mapping_id=m.mapping_id WHERE i.paper_version_id=@VersionId;
SELECT * FROM dbo.COMMENT WHERE paper_version_id=@VersionId ORDER BY comment_id;
SELECT * FROM dbo.vw_RequiredCoverage WHERE course_exam_id=@Exam;
END;
GO

-- Returns version history and decisions for one paper; allows past versions to remain reviewable.
CREATE OR ALTER PROCEDURE dbo.usp_GetPaperVersions
@ActorId int,@PaperId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 DECLARE @Exam int=(SELECT course_exam_id FROM dbo.PAPER WHERE paper_id=@PaperId);
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
SELECT v.*,s.submission_id,s.submitted_at FROM dbo.vw_PaperVersionSummary v LEFT JOIN dbo.PAPER_SUBMISSION s ON s.paper_version_id=v.paper_version_id WHERE v.paper_id=@PaperId ORDER BY v.revision_number DESC;
SELECT r.* FROM dbo.REVIEW r JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id WHERE v.paper_id=@PaperId ORDER BY r.review_id;
END;
GO


-- MODULE: 06_Submission_Review_Receipt.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- HOD records an explained manual alignment check. Merely selecting CLO-1 does not prove alignment.
CREATE OR ALTER PROCEDURE dbo.usp_ReviewCLOMapping
@ActorId int,@MappingId int,@Status nvarchar(30),@Explanation nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int;
SELECT @Dept=o.lead_department_id FROM dbo.CLO_MAPPING m JOIN dbo.EXAM_CLO ec ON ec.exam_clo_id=m.exam_clo_id JOIN dbo.COURSE_EXAM ce ON ce.course_exam_id=ec.course_exam_id JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id WHERE m.mapping_id=@MappingId;
IF @Dept IS NULL THROW 51040,'Mapping not found.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
IF NULLIF(LTRIM(RTRIM(@Explanation)),N'') IS NULL THROW 51040,'Explain the CLO alignment decision.',1;
INSERT dbo.CLO_MAPPING_CHECK(mapping_id,check_status,explanation,checker_version) VALUES(@MappingId,@Status,@Explanation,N'HUMAN-HOD-v1');
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Setter submits atomically after marks, topic and CLO validation. Freezes the version and captures its scope.
CREATE OR ALTER PROCEDURE dbo.usp_SubmitPaper
@ActorId int,@VersionId int,@ExpectedRowVersion binary(8)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireDraftSetter @ActorId,@VersionId,@ExpectedRowVersion;
DECLARE @PaperId int,@Exam int,@Content nvarchar(max),@SubmissionId int;
SELECT @PaperId=p.paper_id,@Exam=p.course_exam_id FROM dbo.PAPER_VERSION v JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE v.paper_version_id=@VersionId;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER WHERE paper_id=@PaperId AND workflow_status=N'DRAFT') THROW 51040,'Only a draft may be submitted.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_PaperVersionSummary WHERE paper_version_id=@VersionId AND main_question_count>0 AND added_marks=required_marks_snapshot) THROW 51040,'MAIN question marks must equal required marks.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND NOT EXISTS(SELECT 1 FROM dbo.QUESTION_PART qp WHERE qp.question_version_id=i.question_version_id AND qp.part_kind=N'ITEM')) THROW 51040,'A MAIN question has no mark-bearing parts.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_PART qp ON qp.question_version_id=i.question_version_id WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND qp.part_kind=N'ITEM' AND NOT EXISTS(SELECT 1 FROM dbo.EXAM_COMMON_TOPIC WHERE course_exam_id=@Exam AND topic_id=qp.topic_id)) THROW 51040,'Question topic is outside the locked common topics.',1;
-- Each ITEM needs a mapping on itself or on its entire question. Group-only mapping does not cover children.
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_PART qp ON qp.question_version_id=i.question_version_id WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND qp.part_kind=N'ITEM'
 AND NOT EXISTS(SELECT 1 FROM dbo.CLO_MAPPING m WHERE m.question_version_id=i.question_version_id AND (m.question_part_id IS NULL OR m.question_part_id=qp.question_part_id))) THROW 51040,'Every scored part needs a question-level or part-level CLO mapping.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.CLO_MAPPING m ON m.question_version_id=i.question_version_id LEFT JOIN dbo.vw_LatestCLOCheck ck ON ck.mapping_id=m.mapping_id WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND (ck.check_status IS NULL OR ck.check_status<>N'ALIGNED')) THROW 51040,'All claimed CLO mappings need an ALIGNED check before submission.',1;
INSERT dbo.PAPER_VERSION_COMMON_TOPIC(paper_version_id,topic_id,topic_title_snapshot) SELECT @VersionId,t.topic_id,t.topic_title FROM dbo.EXAM_COMMON_TOPIC ct JOIN dbo.TOPIC t ON t.topic_id=ct.topic_id WHERE ct.course_exam_id=@Exam;
INSERT dbo.PAPER_TOPIC_CHECK(paper_version_id,question_part_id,common_topic_id,check_status,explanation)
SELECT @VersionId,qp.question_part_id,ct.common_topic_id,N'ELIGIBLE',N'Topic belongs to the locked common-topic set.' FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_PART qp ON qp.question_version_id=i.question_version_id JOIN dbo.EXAM_COMMON_TOPIC ct ON ct.course_exam_id=@Exam AND ct.topic_id=qp.topic_id WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND qp.part_kind=N'ITEM';
SELECT @Content=(SELECT v.instructions,v.required_marks_snapshot,v.duration_minutes_snapshot,
 (SELECT i.placement,i.sort_order,qv.prompt_text,qv.difficulty,
 (SELECT qp.parent_part_id,qp.part_order,qp.part_text,qp.topic_id,qp.marks,qp.difficulty FROM dbo.QUESTION_PART qp WHERE qp.question_version_id=i.question_version_id ORDER BY qp.part_order FOR JSON PATH) AS parts,
 (SELECT m.question_part_id,m.exam_clo_id FROM dbo.CLO_MAPPING m WHERE m.question_version_id=i.question_version_id ORDER BY m.mapping_id FOR JSON PATH) AS mappings,
 (SELECT a.question_part_id,a.display_order,f.checksum FROM dbo.QUESTION_ATTACHMENT a JOIN dbo.FILE_ASSET f ON f.file_asset_id=a.file_asset_id WHERE a.question_version_id=i.question_version_id ORDER BY a.display_order,a.attachment_id FOR JSON PATH) AS attachments
 FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_VERSION qv ON qv.question_version_id=i.question_version_id WHERE i.paper_version_id=@VersionId ORDER BY i.placement,i.sort_order FOR JSON PATH) AS questions
 FROM dbo.PAPER_VERSION v WHERE v.paper_version_id=@VersionId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
UPDATE dbo.PAPER_VERSION SET frozen_at=SYSUTCDATETIME(),content_hash=CONVERT(nvarchar(64),HASHBYTES('SHA2_256',@Content),2) WHERE paper_version_id=@VersionId;
INSERT dbo.PAPER_SUBMISSION(paper_version_id,submission_number,submitted_by_user_id,submitted_at)
SELECT @VersionId,revision_number,@ActorId,SYSUTCDATETIME() FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
SET @SubmissionId=CONVERT(int,SCOPE_IDENTITY());
UPDATE dbo.PAPER SET workflow_status=N'SUBMITTED' WHERE paper_id=@PaperId;
INSERT dbo.AUDIT_EVENT(actor_user_id,entity_type,entity_key,action,occurred_at) VALUES(@ActorId,N'PAPER_VERSION',CONVERT(nvarchar(200),@VersionId),N'SUBMITTED',SYSUTCDATETIME());
SELECT @SubmissionId AS submission_id;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- HOD review first, Director final decision second. Changes requested require feedback and a new submission.
CREATE OR ALTER PROCEDURE dbo.usp_ReviewPaper
@ActorId int,@SubmissionId int,@Stage nvarchar(40),@Decision nvarchar(40),@Feedback nvarchar(max)=NULL,@DelegationId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @PaperId int,@VersionId int,@Dept int,@Status nvarchar(40),@RoleId int,@Setter int;
SELECT @PaperId=p.paper_id,@VersionId=s.paper_version_id,@Dept=o.lead_department_id,@Status=p.workflow_status FROM dbo.PAPER_SUBMISSION s JOIN dbo.PAPER_VERSION v ON v.paper_version_id=s.paper_version_id JOIN dbo.PAPER p WITH(UPDLOCK,HOLDLOCK) ON p.paper_id=v.paper_id JOIN dbo.COURSE_EXAM ce ON ce.course_exam_id=p.course_exam_id JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id WHERE s.submission_id=@SubmissionId;
IF @PaperId IS NULL OR EXISTS(SELECT 1 FROM dbo.PAPER_VERSION WHERE paper_id=@PaperId AND paper_version_id>@VersionId) THROW 51040,'Submission missing or not the latest version.',1;
IF @Stage NOT IN(N'HOD',N'DIRECTOR') OR @Decision NOT IN(N'APPROVED',N'CHANGES_REQUESTED') THROW 51040,'Invalid review stage/decision.',1;
IF (@Stage=N'HOD' AND @Status<>N'SUBMITTED') OR (@Stage=N'DIRECTOR' AND @Status<>N'HOD_REVIEWED') THROW 51040,'Review is out of sequence or already decided.',1;
IF @Stage=N'HOD' BEGIN
 IF @DelegationId IS NOT NULL THROW 51040,'Director delegation does not authorize HOD review.',1;
 EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
 SELECT TOP(1) @RoleId=user_role_id FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId AND role_code=N'HOD' AND (department_id=@Dept OR department_id IS NULL) ORDER BY user_role_id;
END
ELSE BEGIN
 IF @DelegationId IS NULL BEGIN
  EXEC dbo.usp_RequireRole @ActorId,N'DIRECTOR';
  SELECT TOP(1) @RoleId=user_role_id FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId AND role_code=N'DIRECTOR' ORDER BY user_role_id;
 END
 ELSE IF NOT EXISTS(SELECT 1 FROM dbo.DIRECTOR_DELEGATION d JOIN dbo.vw_ActiveUserRole r ON r.user_role_id=d.grantor_user_role_id AND r.role_code=N'DIRECTOR' JOIN dbo.APP_USER u ON u.user_id=d.delegate_user_id AND u.is_active=1 WHERE d.delegation_id=@DelegationId AND d.delegate_user_id=@ActorId AND d.revoked_at IS NULL AND d.starts_at<=SYSUTCDATETIME() AND d.ends_at>SYSUTCDATETIME()) THROW 51040,'Director delegation expired or invalid.',1;
END;
IF @Decision=N'APPROVED' AND EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.CLO_MAPPING m ON m.question_version_id=i.question_version_id LEFT JOIN dbo.vw_LatestCLOCheck ck ON ck.mapping_id=m.mapping_id WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND (ck.check_status IS NULL OR ck.check_status<>N'ALIGNED')) THROW 51040,'CLO checks are not all aligned.',1;
INSERT dbo.REVIEW(submission_id,reviewed_version_id,reviewer_user_id,reviewer_user_role_id,delegation_id,review_stage,decision,feedback,reviewed_at)
VALUES(@SubmissionId,@VersionId,@ActorId,@RoleId,@DelegationId,@Stage,@Decision,@Feedback,SYSUTCDATETIME());
DECLARE @ReviewId int=CONVERT(int,SCOPE_IDENTITY());
UPDATE dbo.PAPER SET workflow_status=CASE WHEN @Decision=N'CHANGES_REQUESTED' THEN N'CHANGES_REQUESTED' WHEN @Stage=N'HOD' THEN N'HOD_REVIEWED' ELSE N'APPROVED' END WHERE paper_id=@PaperId;
SELECT @Setter=setter_user_id FROM dbo.PAPER_ASSIGNMENT WHERE paper_id=@PaperId AND ended_at IS NULL;
INSERT dbo.NOTIFICATION(recipient_user_id,paper_id,event_type,message_text,created_at) VALUES(@Setter,@PaperId,@Decision,CONCAT(@Stage,N': ',@Decision,N'. ',@Feedback),SYSUTCDATETIME());
INSERT dbo.AUDIT_EVENT(actor_user_id,delegation_id,entity_type,entity_key,action,occurred_at) VALUES(@ActorId,@DelegationId,N'REVIEW',CONVERT(nvarchar(200),@ReviewId),@Decision,SYSUTCDATETIME());
SELECT @ReviewId AS review_id;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Adds paper/question feedback without changing frozen paper content.
CREATE OR ALTER PROCEDURE dbo.usp_AddComment
@ActorId int,@VersionId int,@Text nvarchar(max),@PaperItemId int=NULL,@ParentCommentId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Exam int=(SELECT p.course_exam_id FROM dbo.PAPER_VERSION v JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE v.paper_version_id=@VersionId);
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
IF NULLIF(LTRIM(RTRIM(@Text)),N'') IS NULL THROW 51040,'Comment cannot be empty.',1;
IF @ParentCommentId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.COMMENT WHERE comment_id=@ParentCommentId AND paper_version_id=@VersionId) THROW 51040,'Reply must belong to the same version.',1;
INSERT dbo.COMMENT(paper_version_id,paper_item_id,parent_comment_id,author_user_id,target_type,comment_text,created_at)
OUTPUT inserted.comment_id VALUES(@VersionId,@PaperItemId,@ParentCommentId,@ActorId,CASE WHEN @PaperItemId IS NULL THEN N'PAPER' ELSE N'QUESTION' END,@Text,SYSUTCDATETIME());
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Records an API-generated PDF of the approved version; SQL does not generate PDF bytes.
CREATE OR ALTER PROCEDURE dbo.usp_RecordPrintExport
@ActorId int,@ApprovalReviewId int,@FileId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Exam int;
SELECT @Exam=p.course_exam_id FROM dbo.REVIEW r JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE r.review_id=@ApprovalReviewId AND r.review_stage=N'DIRECTOR' AND r.decision=N'APPROVED' AND p.workflow_status IN(N'APPROVED',N'RECEIVED');
IF @Exam IS NULL THROW 51040,'A final Director approval is required.',1;
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
IF NOT EXISTS(SELECT 1 FROM dbo.FILE_ASSET WHERE file_asset_id=@FileId AND uploaded_by_user_id=@ActorId AND mime_type=N'application/pdf') THROW 51040,'Register your generated PDF first.',1;
INSERT dbo.PRINT_EXPORT(approval_review_id,requested_by_user_id,file_asset_id,generated_at) OUTPUT inserted.print_export_id VALUES(@ApprovalReviewId,@ActorId,@FileId,SYSUTCDATETIME());
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Data Cell records the sealed physical paper receipt once; duplicate receipt is rejected.
CREATE OR ALTER PROCEDURE dbo.usp_MarkPaperReceived
@ActorId int,@ApprovalReviewId int,@DeliveredBy int,@Remarks nvarchar(max)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
DECLARE @PaperId int;
SELECT @PaperId=p.paper_id FROM dbo.REVIEW r JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id JOIN dbo.PAPER p WITH(UPDLOCK,HOLDLOCK) ON p.paper_id=v.paper_id WHERE r.review_id=@ApprovalReviewId AND r.review_stage=N'DIRECTOR' AND r.decision=N'APPROVED' AND p.workflow_status=N'APPROVED';
IF @PaperId IS NULL THROW 51040,'Paper is not approved or was already received.',1;
INSERT dbo.PHYSICAL_RECEIPT(approval_review_id,received_by_user_id,delivered_by_user_id,received_at,seal_status,remarks) VALUES(@ApprovalReviewId,@ActorId,@DeliveredBy,SYSUTCDATETIME(),N'SEALED',@Remarks);
UPDATE dbo.PAPER SET workflow_status=N'RECEIVED' WHERE paper_id=@PaperId;
SELECT * FROM dbo.PHYSICAL_RECEIPT WHERE approval_review_id=@ApprovalReviewId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO


-- MODULE: 07_Similarity_And_Checking.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Calculates exact reuse in SQL: 3/4=75%. Separate percentage per past paper; never sums percentages. Recheck after any draft edit.
CREATE OR ALTER PROCEDURE dbo.usp_RunExactSimilarity
@ActorId int,@VersionId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Exam int,@Course int,@SessionStart date,@RunId int,@Total int,@Hash nvarchar(256);
SELECT @Exam=p.course_exam_id,@Course=o.course_id,@SessionStart=s.starts_on,@Hash=v.content_hash FROM dbo.PAPER_VERSION v WITH(UPDLOCK,HOLDLOCK) JOIN dbo.PAPER p ON p.paper_id=v.paper_id JOIN dbo.COURSE_EXAM ce ON ce.course_exam_id=p.course_exam_id JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id JOIN dbo.ACADEMIC_SESSION s ON s.session_id=o.session_id WHERE v.paper_version_id=@VersionId;
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
SELECT @Total=COUNT(*) FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND placement=N'MAIN';
IF @Total=0 THROW 51050,'Add at least one MAIN question before checking similarity.',1;
-- Corpus: latest Director-approved version of each same-course paper in earlier sessions.
DECLARE @Sources TABLE(version_id int PRIMARY KEY);
INSERT @Sources
SELECT MAX(r.reviewed_version_id) FROM dbo.REVIEW r JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id JOIN dbo.PAPER p ON p.paper_id=v.paper_id JOIN dbo.COURSE_EXAM ce ON ce.course_exam_id=p.course_exam_id JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id JOIN dbo.ACADEMIC_SESSION s ON s.session_id=o.session_id
WHERE r.review_stage=N'DIRECTOR' AND r.decision=N'APPROVED' AND o.course_id=@Course AND s.starts_on<@SessionStart AND p.deleted_at IS NULL GROUP BY p.paper_id;
DECLARE @Corpus nvarchar(max)=(SELECT CONVERT(varchar(18),row_version,1) AS workspace_token,JSON_QUERY((SELECT version_id FROM @Sources ORDER BY version_id FOR JSON PATH)) AS sources FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT dbo.SIMILARITY_RUN(paper_version_id,match_mode,algorithm_version,corpus_scope,corpus_hash,aggregation_method,run_status)
VALUES(@VersionId,N'EXACT',N'ExactText-v1: trim outer spaces; case-sensitive; stem and ordered part text/tree; ignore marks/CLOs',@Corpus,CONVERT(nvarchar(64),HASHBYTES('SHA2_256',@Corpus),2),N'Distinct current MAIN questions per source / total current MAIN questions',N'PENDING');
SET @RunId=CONVERT(int,SCOPE_IDENTITY());
DECLARE @Signatures TABLE(item_id int PRIMARY KEY,version_id int NOT NULL,sig varbinary(32) NOT NULL);
INSERT @Signatures
SELECT i.paper_item_id,i.paper_version_id,HASHBYTES('SHA2_256',
 (SELECT LTRIM(RTRIM(qv.prompt_text)) AS stem,
 (SELECT qp.part_kind,qp.part_label,LTRIM(RTRIM(qp.part_text)) AS body,parent.part_label AS parent_label,
 ROW_NUMBER() OVER(ORDER BY qp.part_order) AS position
 FROM dbo.QUESTION_PART qp LEFT JOIN dbo.QUESTION_PART parent ON parent.question_part_id=qp.parent_part_id
 WHERE qp.question_version_id=i.question_version_id ORDER BY qp.part_order FOR JSON PATH) AS parts,
 (SELECT f.checksum,a.display_order,qp.part_label FROM dbo.QUESTION_ATTACHMENT a JOIN dbo.FILE_ASSET f ON f.file_asset_id=a.file_asset_id LEFT JOIN dbo.QUESTION_PART qp ON qp.question_part_id=a.question_part_id WHERE a.question_version_id=i.question_version_id ORDER BY a.display_order,a.attachment_id FOR JSON PATH) AS attachments
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER))
FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_VERSION qv ON qv.question_version_id=i.question_version_id
WHERE i.placement=N'MAIN' AND (i.paper_version_id=@VersionId OR EXISTS(SELECT 1 FROM @Sources s WHERE s.version_id=i.paper_version_id));
INSERT dbo.QUESTION_MATCH(similarity_run_id,current_item_id,source_item_id,similarity_percent)
SELECT @RunId,c.item_id,s.item_id,100 FROM @Signatures c JOIN @Signatures s ON s.sig=c.sig AND s.version_id<>@VersionId WHERE c.version_id=@VersionId;
INSERT dbo.PAPER_MATCH(similarity_run_id,source_paper_version_id,similarity_percent)
SELECT @RunId,src.version_id,CONVERT(decimal(10,2),100.0*COUNT(DISTINCT qm.current_item_id)/@Total)
FROM @Sources src LEFT JOIN @Signatures sig ON sig.version_id=src.version_id LEFT JOIN dbo.QUESTION_MATCH qm ON qm.source_item_id=sig.item_id AND qm.similarity_run_id=@RunId GROUP BY src.version_id;
UPDATE dbo.SIMILARITY_RUN SET run_status=N'COMPLETED',computed_at=SYSUTCDATETIME(),overall_percent=(SELECT CONVERT(decimal(10,2),100.0*COUNT(DISTINCT current_item_id)/@Total) FROM dbo.QUESTION_MATCH WHERE similarity_run_id=@RunId) WHERE similarity_run_id=@RunId;
SELECT @RunId AS similarity_run_id,@Total AS total_main_questions;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Bottom-sheet report: header, percentages by source, and matched question pairs.
CREATE OR ALTER PROCEDURE dbo.usp_GetSimilarityReport
@ActorId int,@RunId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 DECLARE @VersionId int,@Exam int;
SELECT @VersionId=sr.paper_version_id,@Exam=p.course_exam_id FROM dbo.SIMILARITY_RUN sr JOIN dbo.PAPER_VERSION v ON v.paper_version_id=sr.paper_version_id JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE sr.similarity_run_id=@RunId;
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
IF EXISTS(SELECT 1 FROM dbo.SIMILARITY_RUN sr JOIN dbo.PAPER_VERSION v ON v.paper_version_id=sr.paper_version_id WHERE sr.similarity_run_id=@RunId AND JSON_VALUE(sr.corpus_scope,'$.workspace_token')<>CONVERT(varchar(18),v.row_version,1)) THROW 51050,'Paper changed after this check; run similarity again.',1;
SELECT sr.*,ce.course_code,ce.course_title,ce.session_name,ce.exam_type,v.revision_number FROM dbo.SIMILARITY_RUN sr JOIN dbo.PAPER_VERSION v ON v.paper_version_id=sr.paper_version_id JOIN dbo.PAPER p ON p.paper_id=v.paper_id JOIN dbo.vw_CourseExam ce ON ce.course_exam_id=p.course_exam_id WHERE sr.similarity_run_id=@RunId;
SELECT pm.source_paper_version_id,ce.session_name,ce.exam_type,ce.course_code,v.revision_number,pm.similarity_percent,
 (SELECT COUNT(DISTINCT qm.current_item_id) FROM dbo.QUESTION_MATCH qm JOIN dbo.PAPER_ITEM si ON si.paper_item_id=qm.source_item_id WHERE qm.similarity_run_id=@RunId AND si.paper_version_id=pm.source_paper_version_id) AS matched_current_questions,
 (SELECT COUNT(*) FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND placement=N'MAIN') AS current_main_questions
FROM dbo.PAPER_MATCH pm JOIN dbo.PAPER_VERSION v ON v.paper_version_id=pm.source_paper_version_id JOIN dbo.PAPER p ON p.paper_id=v.paper_id JOIN dbo.vw_CourseExam ce ON ce.course_exam_id=p.course_exam_id WHERE pm.similarity_run_id=@RunId ORDER BY pm.similarity_percent DESC;
SELECT qm.current_item_id,ci.sort_order AS current_question_number,cq.prompt_text AS current_question,qm.source_item_id,si.paper_version_id AS source_version_id,si.sort_order AS source_question_number,sq.prompt_text AS source_question,qm.similarity_percent
FROM dbo.QUESTION_MATCH qm JOIN dbo.PAPER_ITEM ci ON ci.paper_item_id=qm.current_item_id JOIN dbo.QUESTION_VERSION cq ON cq.question_version_id=ci.question_version_id JOIN dbo.PAPER_ITEM si ON si.paper_item_id=qm.source_item_id JOIN dbo.QUESTION_VERSION sq ON sq.question_version_id=si.question_version_id WHERE qm.similarity_run_id=@RunId ORDER BY ci.sort_order,si.paper_version_id;
END;
GO

-- Checker service ONLY: fetch immutable question revision IDs, part text and claimed CLO statements.
CREATE OR ALTER PROCEDURE dbo.usp_GetCheckingPayload
@VersionId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 SELECT i.paper_item_id,i.question_version_id,qv.prompt_text FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_VERSION qv ON qv.question_version_id=i.question_version_id WHERE i.paper_version_id=@VersionId;
SELECT qp.* FROM dbo.QUESTION_PART qp JOIN dbo.PAPER_ITEM i ON i.question_version_id=qp.question_version_id WHERE i.paper_version_id=@VersionId;
SELECT m.mapping_id,m.question_version_id,m.question_part_id,c.clo_statement FROM dbo.CLO_MAPPING m JOIN dbo.PAPER_ITEM i ON i.question_version_id=m.question_version_id JOIN dbo.EXAM_CLO ec ON ec.exam_clo_id=m.exam_clo_id JOIN dbo.CLO c ON c.clo_id=ec.clo_id WHERE i.paper_version_id=@VersionId;
SELECT a.attachment_id,a.question_version_id,a.question_part_id,f.storage_key,f.mime_type,f.checksum
FROM dbo.QUESTION_ATTACHMENT a JOIN dbo.PAPER_ITEM i ON i.question_version_id=a.question_version_id
JOIN dbo.FILE_ASSET f ON f.file_asset_id=a.file_asset_id WHERE i.paper_version_id=@VersionId;
END;
GO

-- Checker service ONLY: stores explained alignment results. SQL does not infer semantic alignment.
CREATE OR ALTER PROCEDURE dbo.usp_SaveCLOCheckResults
@VersionId int,@CheckerVersion nvarchar(100),@ResultsJson nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
IF ISJSON(@ResultsJson)<>1 OR NULLIF(@CheckerVersion,N'') IS NULL THROW 51050,'Checker version and results JSON required.',1;
DECLARE @Results TABLE(mapping_id int PRIMARY KEY,status nvarchar(30) NOT NULL,explanation nvarchar(max) NOT NULL);
INSERT @Results SELECT mapping_id,status,explanation FROM OPENJSON(@ResultsJson) WITH(mapping_id int,status nvarchar(30),explanation nvarchar(max));
IF EXISTS(SELECT 1 FROM @Results x WHERE NOT EXISTS(SELECT 1 FROM dbo.CLO_MAPPING m JOIN dbo.PAPER_ITEM i ON i.question_version_id=m.question_version_id WHERE m.mapping_id=x.mapping_id AND i.paper_version_id=@VersionId)) THROW 51050,'Result does not belong to this version.',1;
INSERT dbo.CLO_MAPPING_CHECK(mapping_id,check_status,explanation,checker_version) SELECT mapping_id,status,explanation,@CheckerVersion FROM @Results;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Queues a semantic check; API worker computes scores. Semantic percentages are not exact question reuse.
CREATE OR ALTER PROCEDURE dbo.usp_StartSemanticSimilarity
@ActorId int,@VersionId int,@AlgorithmVersion nvarchar(200)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Exam int,@Course int,@Start date,@Corpus nvarchar(max),@RunId int;
SELECT @Exam=p.course_exam_id,@Course=o.course_id,@Start=s.starts_on FROM dbo.PAPER_VERSION v WITH(UPDLOCK,HOLDLOCK) JOIN dbo.PAPER p ON p.paper_id=v.paper_id JOIN dbo.COURSE_EXAM ce ON ce.course_exam_id=p.course_exam_id JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id JOIN dbo.ACADEMIC_SESSION s ON s.session_id=o.session_id WHERE v.paper_version_id=@VersionId;
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND placement=N'MAIN') THROW 51050,'MAIN questions required.',1;
DECLARE @Sources TABLE(version_id int PRIMARY KEY);
INSERT @Sources SELECT MAX(r.reviewed_version_id) FROM dbo.REVIEW r JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id JOIN dbo.PAPER p ON p.paper_id=v.paper_id JOIN dbo.COURSE_EXAM ce ON ce.course_exam_id=p.course_exam_id JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id JOIN dbo.ACADEMIC_SESSION s ON s.session_id=o.session_id WHERE r.review_stage=N'DIRECTOR' AND r.decision=N'APPROVED' AND o.course_id=@Course AND s.starts_on<@Start AND p.deleted_at IS NULL GROUP BY p.paper_id;
SELECT @Corpus=(SELECT CONVERT(varchar(18),row_version,1) AS workspace_token,JSON_QUERY((SELECT version_id FROM @Sources ORDER BY version_id FOR JSON PATH)) AS sources FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT dbo.SIMILARITY_RUN(paper_version_id,match_mode,algorithm_version,corpus_scope,corpus_hash,aggregation_method,run_status)
VALUES(@VersionId,N'SEMANTIC',@AlgorithmVersion,@Corpus,CONVERT(nvarchar(64),HASHBYTES('SHA2_256',@Corpus),2),N'Average best source-question score per current MAIN question; absent pairs=0',N'PENDING');
SET @RunId=CONVERT(int,SCOPE_IDENTITY());
SELECT @RunId AS similarity_run_id,@Corpus AS corpus_scope;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Checker service ONLY: validates semantic pairs and calculates per-source averages; omitted pairs score zero.
CREATE OR ALTER PROCEDURE dbo.usp_CompleteSemanticSimilarity
@RunId int,@MatchesJson nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Version int,@Scope nvarchar(max),@Total int;
SELECT @Version=paper_version_id,@Scope=corpus_scope FROM dbo.SIMILARITY_RUN WITH(UPDLOCK,HOLDLOCK) WHERE similarity_run_id=@RunId AND match_mode=N'SEMANTIC' AND run_status=N'PENDING';
IF @Version IS NULL OR ISJSON(@MatchesJson)<>1 THROW 51050,'Pending semantic run and JSON results required.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER_VERSION WHERE paper_version_id=@Version AND CONVERT(varchar(18),row_version,1)=JSON_VALUE(@Scope,'$.workspace_token')) THROW 51050,'Draft changed during the check. Start again.',1;
DECLARE @Matches TABLE(current_item_id int,source_item_id int,score decimal(10,2) NOT NULL,PRIMARY KEY(current_item_id,source_item_id));
INSERT @Matches SELECT current_item_id,source_item_id,score FROM OPENJSON(@MatchesJson) WITH(current_item_id int,source_item_id int,score decimal(10,2));
IF EXISTS(SELECT 1 FROM @Matches m WHERE score<0 OR score>100
 OR NOT EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i WHERE i.paper_item_id=m.current_item_id AND i.paper_version_id=@Version AND i.placement=N'MAIN')
 OR NOT EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN OPENJSON(@Scope,'$.sources') WITH(version_id int) src ON src.version_id=i.paper_version_id WHERE i.paper_item_id=m.source_item_id AND i.placement=N'MAIN')) THROW 51050,'Invalid score or current/source question outside the corpus.',1;
SELECT @Total=COUNT(*) FROM dbo.PAPER_ITEM WHERE paper_version_id=@Version AND placement=N'MAIN';
INSERT dbo.QUESTION_MATCH(similarity_run_id,current_item_id,source_item_id,similarity_percent) SELECT @RunId,current_item_id,source_item_id,score FROM @Matches;
INSERT dbo.PAPER_MATCH(similarity_run_id,source_paper_version_id,similarity_percent)
SELECT @RunId,src.version_id,CONVERT(decimal(10,2),SUM(COALESCE(best.score,0))/@Total)
FROM OPENJSON(@Scope,'$.sources') WITH(version_id int) src CROSS JOIN dbo.PAPER_ITEM cur
OUTER APPLY(SELECT MAX(m.score) AS score FROM @Matches m JOIN dbo.PAPER_ITEM si ON si.paper_item_id=m.source_item_id WHERE m.current_item_id=cur.paper_item_id AND si.paper_version_id=src.version_id) best
WHERE cur.paper_version_id=@Version AND cur.placement=N'MAIN' GROUP BY src.version_id;
UPDATE dbo.SIMILARITY_RUN SET run_status=N'COMPLETED',computed_at=SYSUTCDATETIME(),overall_percent=(SELECT CONVERT(decimal(10,2),SUM(COALESCE(x.score,0))/@Total) FROM dbo.PAPER_ITEM ci OUTER APPLY(SELECT MAX(score) AS score FROM @Matches m WHERE m.current_item_id=ci.paper_item_id) x WHERE ci.paper_version_id=@Version AND ci.placement=N'MAIN') WHERE similarity_run_id=@RunId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Checker service ONLY: marks a failed/cancelled pending check so UI can offer retry.
CREATE OR ALTER PROCEDURE dbo.usp_FailSimilarityRun
@RunId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
UPDATE dbo.SIMILARITY_RUN SET run_status=N'FAILED',computed_at=SYSUTCDATETIME() WHERE similarity_run_id=@RunId AND run_status=N'PENDING';
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO


-- MODULE: 08_Dashboard_And_Utilities.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Role-scoped dashboard counts. Optional historic filter does not alter the global current exam.
CREATE OR ALTER PROCEDURE dbo.usp_GetDashboard
@ActorId int,@ExamCycleId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId) THROW 51001,'Active role required.',1;
SET @ExamCycleId=COALESCE(@ExamCycleId,(SELECT active_exam_cycle_id FROM dbo.SYSTEM_SETTINGS WHERE settings_id=1));
SELECT @ExamCycleId AS exam_cycle_id;
SELECT workflow_status,COUNT(*) AS paper_count FROM dbo.vw_PaperList p WHERE p.exam_cycle_id=@ExamCycleId AND p.deleted_at IS NULL AND dbo.fn_CanAccessExam(@ActorId,p.course_exam_id)=1 GROUP BY workflow_status;
SELECT COUNT(*) AS unread_notifications FROM dbo.NOTIFICATION WHERE recipient_user_id=@ActorId AND read_at IS NULL;
END;
GO

-- Paginated current/past paper list with status and course search filters.
CREATE OR ALTER PROCEDURE dbo.usp_ListPapers
@ActorId int,@ExamCycleId int=NULL,@Status nvarchar(40)=NULL,@Search nvarchar(200)=NULL,@Page int=1,@PageSize int=20
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @Page<1 OR @PageSize<1 OR @PageSize>100 THROW 51060,'Page >=1; page size 1..100.',1;
SET @ExamCycleId=COALESCE(@ExamCycleId,(SELECT active_exam_cycle_id FROM dbo.SYSTEM_SETTINGS WHERE settings_id=1));
SELECT p.*,COUNT(*) OVER() AS total_rows FROM dbo.vw_PaperList p WHERE p.exam_cycle_id=@ExamCycleId AND p.deleted_at IS NULL AND dbo.fn_CanAccessExam(@ActorId,p.course_exam_id)=1
 AND (@Status IS NULL OR p.workflow_status=@Status) AND (@Search IS NULL OR p.course_title LIKE N'%'+@Search+N'%' OR p.course_code LIKE N'%'+@Search+N'%')
ORDER BY p.paper_id DESC OFFSET (@Page-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- Courses for the active role and selected/default exam; includes courses not yet assigned a paper.
CREATE OR ALTER PROCEDURE dbo.usp_ListMyCourses
@ActorId int,@ExamCycleId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 SET @ExamCycleId=COALESCE(@ExamCycleId,(SELECT active_exam_cycle_id FROM dbo.SYSTEM_SETTINGS WHERE settings_id=1));
SELECT ce.*,p.paper_id,p.workflow_status FROM dbo.vw_CourseExam ce LEFT JOIN dbo.PAPER p ON p.course_exam_id=ce.course_exam_id WHERE ce.exam_cycle_id=@ExamCycleId AND dbo.fn_CanAccessExam(@ActorId,ce.course_exam_id)=1 ORDER BY ce.course_code;
END;
GO

-- Returns the current user's latest 100 notifications.
CREATE OR ALTER PROCEDURE dbo.usp_GetNotifications
@ActorId int,@UnreadOnly bit=0
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 SELECT TOP(100) notification_id,paper_id,delegation_id,event_type,message_text,created_at,read_at FROM dbo.NOTIFICATION WHERE recipient_user_id=@ActorId AND (@UnreadOnly=0 OR read_at IS NULL) ORDER BY notification_id DESC;
END;
GO

-- Marks only a notification belonging to this user as read.
CREATE OR ALTER PROCEDURE dbo.usp_MarkNotificationRead
@ActorId int,@NotificationId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
UPDATE dbo.NOTIFICATION SET read_at=COALESCE(read_at,SYSUTCDATETIME()) WHERE notification_id=@NotificationId AND recipient_user_id=@ActorId;
IF @@ROWCOUNT=0 THROW 51060,'Notification not found.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Creates/edits a private wishlist/task item; optional paper association is access checked.
CREATE OR ALTER PROCEDURE dbo.usp_SaveWishlist
@ActorId int,@Title nvarchar(500),@Note nvarchar(max)=NULL,@DueAt datetime2(0)=NULL,@WishlistId int=NULL,@PaperId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
IF NOT EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId) THROW 51001,'Active role required.',1;
IF @PaperId IS NOT NULL BEGIN
 DECLARE @Exam int=(SELECT course_exam_id FROM dbo.PAPER WHERE paper_id=@PaperId); EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
END;
IF @WishlistId IS NULL BEGIN
 INSERT dbo.WISHLIST(owner_user_id,paper_id,title,note_text,due_at) VALUES(@ActorId,@PaperId,@Title,@Note,@DueAt); SET @WishlistId=CONVERT(int,SCOPE_IDENTITY());
END
ELSE BEGIN
 UPDATE dbo.WISHLIST SET title=@Title,note_text=@Note,due_at=@DueAt WHERE wishlist_id=@WishlistId AND owner_user_id=@ActorId AND deleted_at IS NULL;
 IF @@ROWCOUNT=0 THROW 51060,'Wishlist item not found.',1;
END;
SELECT @WishlistId AS wishlist_id;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Completes/uncompletes, soft-deletes or restores the user's own wishlist item.
CREATE OR ALTER PROCEDURE dbo.usp_SetWishlistState
@ActorId int,@WishlistId int,@Action nvarchar(20)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
IF @Action NOT IN(N'DONE',N'UNDONE',N'TRASH',N'RESTORE') THROW 51060,'Unknown wishlist action.',1;
UPDATE dbo.WISHLIST SET is_done=CASE WHEN @Action=N'DONE' THEN 1 WHEN @Action=N'UNDONE' THEN 0 ELSE is_done END,
 deleted_at=CASE WHEN @Action=N'TRASH' THEN SYSUTCDATETIME() WHEN @Action=N'RESTORE' THEN NULL ELSE deleted_at END,
 deleted_by_user_id=CASE WHEN @Action=N'TRASH' THEN @ActorId WHEN @Action=N'RESTORE' THEN NULL ELSE deleted_by_user_id END
WHERE wishlist_id=@WishlistId AND owner_user_id=@ActorId;
IF @@ROWCOUNT=0 THROW 51060,'Wishlist item not found.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Returns active wishlist or trash without deleting history.
CREATE OR ALTER PROCEDURE dbo.usp_GetWishlist
@ActorId int,@Trash bit=0
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 SELECT * FROM dbo.WISHLIST WHERE owner_user_id=@ActorId AND ((@Trash=0 AND deleted_at IS NULL) OR (@Trash=1 AND deleted_at IS NOT NULL)) ORDER BY wishlist_id DESC;
END;
GO

-- Director grants time-limited delegated approval authority to a faculty member.
CREATE OR ALTER PROCEDURE dbo.usp_GrantDirectorDelegation
@ActorId int,@DelegateId int,@StartsAt datetime2(0),@EndsAt datetime2(0),@Reason nvarchar(500)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DIRECTOR';
IF @DelegateId=@ActorId OR @EndsAt<=SYSUTCDATETIME() OR NOT EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@DelegateId AND role_code IN(N'TEACHER',N'HOD')) THROW 51060,'Choose another active faculty member and a future expiry.',1;
DECLARE @RoleId int=(SELECT TOP(1) user_role_id FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId AND role_code=N'DIRECTOR' ORDER BY user_role_id);
INSERT dbo.DIRECTOR_DELEGATION(grantor_user_role_id,delegate_user_id,starts_at,ends_at,reason) OUTPUT inserted.delegation_id VALUES(@RoleId,@DelegateId,@StartsAt,@EndsAt,@Reason);
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Director immediately revokes delegation while retaining history.
CREATE OR ALTER PROCEDURE dbo.usp_RevokeDirectorDelegation
@ActorId int,@DelegationId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DIRECTOR';
UPDATE dbo.DIRECTOR_DELEGATION SET revoked_at=SYSUTCDATETIME(),revoked_by_user_id=@ActorId WHERE delegation_id=@DelegationId AND revoked_at IS NULL;
IF @@ROWCOUNT=0 THROW 51060,'Delegation not found or already revoked.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Director sees delegation history; a delegate sees their own grants and expiry status.
CREATE OR ALTER PROCEDURE dbo.usp_GetDelegations
@ActorId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId) THROW 51001,'Active role required.',1;
SELECT d.*,u.full_name AS delegate_name,
 CASE WHEN d.revoked_at IS NOT NULL THEN N'REVOKED' WHEN d.ends_at<=SYSUTCDATETIME() THEN N'EXPIRED' WHEN d.starts_at>SYSUTCDATETIME() THEN N'SCHEDULED' ELSE N'ACTIVE' END AS status
FROM dbo.DIRECTOR_DELEGATION d JOIN dbo.APP_USER u ON u.user_id=d.delegate_user_id
WHERE d.delegate_user_id=@ActorId OR EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId AND role_code=N'DIRECTOR') ORDER BY d.delegation_id DESC;
END;
GO

-- Begins an import log after the API uploads/parses an Excel file; does not publish a datesheet.
CREATE OR ALTER PROCEDURE dbo.usp_StartImport
@ActorId int,@FileId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
IF NOT EXISTS(SELECT 1 FROM dbo.FILE_ASSET WHERE file_asset_id=@FileId AND uploaded_by_user_id=@ActorId) THROW 51060,'Import file does not belong to you.',1;
INSERT dbo.IMPORT_BATCH(file_asset_id,imported_by_user_id,import_status,rows_added,rows_updated,rows_failed,started_at) OUTPUT inserted.import_batch_id VALUES(@FileId,@ActorId,N'PENDING',0,0,0,SYSUTCDATETIME());
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Finishes an import log with counts and row-level errors after calling academic setup procedures.
CREATE OR ALTER PROCEDURE dbo.usp_FinishImport
@ActorId int,@ImportId int,@Status nvarchar(40),@Added int,@Updated int,@ErrorsJson nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
IF @Status NOT IN(N'COMPLETED',N'PARTIAL',N'FAILED') OR ISJSON(@ErrorsJson)<>1 THROW 51060,'Invalid final status/errors JSON.',1;
UPDATE dbo.IMPORT_BATCH SET import_status=@Status,rows_added=@Added,rows_updated=@Updated,rows_failed=(SELECT COUNT(*) FROM OPENJSON(@ErrorsJson)),completed_at=SYSUTCDATETIME() WHERE import_batch_id=@ImportId AND imported_by_user_id=@ActorId AND import_status=N'PENDING';
IF @@ROWCOUNT=0 THROW 51060,'Pending import not found.',1;
INSERT dbo.IMPORT_ERROR(import_batch_id,sheet_name,row_number,column_name,error_message) SELECT @ImportId,sheet_name,row_number,column_name,error_message FROM OPENJSON(@ErrorsJson) WITH(sheet_name nvarchar(200),row_number int,column_name nvarchar(200),error_message nvarchar(max));
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO


-- MODULE: 09_Maintenance.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Corrects draft topic text/order; parent changes require a new draft topic to avoid hierarchy cycles.
CREATE OR ALTER PROCEDURE dbo.usp_UpdateOutlineTopic
@ActorId int,@TopicId int,@Title nvarchar(200),@Week int=NULL,@SortOrder int=1
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int;
SELECT @Dept=c.owner_department_id FROM dbo.TOPIC t JOIN dbo.COURSE_OUTLINE o WITH(UPDLOCK,HOLDLOCK) ON o.outline_id=t.outline_id JOIN dbo.COURSE c ON c.course_id=o.course_id WHERE t.topic_id=@TopicId AND o.published_at IS NULL;
IF @Dept IS NULL THROW 51010,'Topic missing or outline already published.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
UPDATE dbo.TOPIC SET topic_title=@Title,week_number=@Week,sort_order=@SortOrder WHERE topic_id=@TopicId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Corrects a CLO statement only before outline publication.
CREATE OR ALTER PROCEDURE dbo.usp_UpdateOutlineCLO
@ActorId int,@CloId int,@Code nvarchar(40),@Statement nvarchar(max)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int;
SELECT @Dept=c.owner_department_id FROM dbo.CLO clo JOIN dbo.COURSE_OUTLINE o WITH(UPDLOCK,HOLDLOCK) ON o.outline_id=clo.outline_id JOIN dbo.COURSE c ON c.course_id=o.course_id WHERE clo.clo_id=@CloId AND o.published_at IS NULL;
IF @Dept IS NULL THROW 51010,'CLO missing or outline already published.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
UPDATE dbo.CLO SET clo_code=@Code,clo_statement=@Statement WHERE clo_id=@CloId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Corrects exam configuration before drafting starts; version snapshots will not be silently overwritten.
CREATE OR ALTER PROCEDURE dbo.usp_UpdateCourseExamSettings
@ActorId int,@ExamId int,@Marks decimal(10,2),@Duration int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @Dept int;
SELECT @Dept=o.lead_department_id FROM dbo.COURSE_EXAM ce WITH(UPDLOCK,HOLDLOCK) JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id WHERE ce.course_exam_id=@ExamId;
IF @Dept IS NULL THROW 51010,'Exam not found.',1;
EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
IF EXISTS(SELECT 1 FROM dbo.PAPER p JOIN dbo.PAPER_VERSION v ON v.paper_id=p.paper_id WHERE p.course_exam_id=@ExamId) THROW 51010,'Cannot change settings after a paper version exists.',1;
UPDATE dbo.COURSE_EXAM SET required_marks=@Marks,duration_minutes=@Duration WHERE course_exam_id=@ExamId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Ends an assignment before scope lock; then assign a replacement teacher with usp_AssignTeaching.
CREATE OR ALTER PROCEDURE dbo.usp_EndTeachingAssignment
@ActorId int,@TeachingAssignmentId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
IF EXISTS(SELECT 1 FROM dbo.TEACHING_ASSIGNMENT ta JOIN dbo.OFFERING_SECTION os ON os.offering_section_id=ta.offering_section_id JOIN dbo.COURSE_EXAM ce WITH(UPDLOCK,HOLDLOCK) ON ce.offering_id=os.offering_id WHERE ta.teaching_assignment_id=@TeachingAssignmentId AND ce.scope_locked_at IS NOT NULL) THROW 51010,'Teaching scope is locked; cannot change the common-topic participants.',1;
UPDATE dbo.TEACHING_ASSIGNMENT SET ends_at=SYSUTCDATETIME() WHERE teaching_assignment_id=@TeachingAssignmentId AND ends_at IS NULL AND starts_at<SYSUTCDATETIME();
IF @@ROWCOUNT=0 THROW 51010,'Open assignment not found or created in the same second.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Dropdown data for academic setup; deliberately excludes password hashes.
CREATE OR ALTER PROCEDURE dbo.usp_GetAcademicLookups
@ActorId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId AND role_code IN(N'DATA_CELL',N'HOD',N'DIRECTOR')) THROW 51001,'Administrative role required.',1;
SELECT * FROM dbo.DEPARTMENT ORDER BY department_name;
SELECT * FROM dbo.PROGRAM ORDER BY program_name;
SELECT * FROM dbo.ACADEMIC_SESSION ORDER BY starts_on DESC;
SELECT * FROM dbo.EXAM_CYCLE ORDER BY exam_cycle_id DESC;
SELECT * FROM dbo.COURSE ORDER BY course_code;
SELECT * FROM dbo.SECTION ORDER BY session_id,section_name;
SELECT * FROM dbo.COURSE_OUTLINE ORDER BY course_id,version_number DESC;
SELECT user_id,staff_number,full_name,email,designation,home_department_id,is_active FROM dbo.APP_USER ORDER BY full_name;
END;
GO

-- Lists recent academic imports and optional row errors for one selected import.
CREATE OR ALTER PROCEDURE dbo.usp_GetImportHistory
@ActorId int,@ImportId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
SELECT TOP(100) b.*,f.original_name FROM dbo.IMPORT_BATCH b JOIN dbo.FILE_ASSET f ON f.file_asset_id=b.file_asset_id WHERE @ImportId IS NULL OR b.import_batch_id=@ImportId ORDER BY b.import_batch_id DESC;
SELECT e.* FROM dbo.IMPORT_ERROR e WHERE e.import_batch_id=@ImportId ORDER BY e.row_number;
END;
GO

-- Archives a received paper without deleting its versions or similarity history.
CREATE OR ALTER PROCEDURE dbo.usp_ArchivePaper
@ActorId int,@PaperId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
UPDATE dbo.PAPER SET workflow_status=N'ARCHIVED',archived_at=SYSUTCDATETIME() WHERE paper_id=@PaperId AND workflow_status=N'RECEIVED';
IF @@ROWCOUNT=0 THROW 51060,'Only a received paper can be archived.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO


-- MODULE: 09b_Attachments_And_Staff.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Setter attaches an uploaded diagram/file to a draft question/part without modifying frozen revisions.
CREATE OR ALTER PROCEDURE dbo.usp_AddQuestionAttachment
@ActorId int,@VersionId int,@ExpectedRowVersion binary(8),@QuestionVersionId int,@FileId int,@PartId int=NULL,@DisplayOrder int=1
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireDraftSetter @ActorId,@VersionId,@ExpectedRowVersion;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND question_version_id=@QuestionVersionId) THROW 51031,'Question is not in this draft.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.PAPER_VERSION v ON v.paper_version_id=i.paper_version_id WHERE i.question_version_id=@QuestionVersionId AND v.frozen_at IS NOT NULL) THROW 51031,'Save a new question revision before changing attachments reused by a frozen paper.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.FILE_ASSET WHERE file_asset_id=@FileId AND uploaded_by_user_id=@ActorId) THROW 51031,'Register your upload first.',1;
INSERT dbo.QUESTION_ATTACHMENT(question_version_id,question_part_id,file_asset_id,display_order) OUTPUT inserted.attachment_id VALUES(@QuestionVersionId,@PartId,@FileId,@DisplayOrder);
UPDATE dbo.PAPER_VERSION SET content_hash=N'DRAFT' WHERE paper_version_id=@VersionId;
SELECT row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Removes a draft attachment link, retaining the file asset and historical files.
CREATE OR ALTER PROCEDURE dbo.usp_RemoveQuestionAttachment
@ActorId int,@VersionId int,@ExpectedRowVersion binary(8),@AttachmentId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireDraftSetter @ActorId,@VersionId,@ExpectedRowVersion;
DECLARE @QV int=(SELECT question_version_id FROM dbo.QUESTION_ATTACHMENT WHERE attachment_id=@AttachmentId);
IF @QV IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND question_version_id=@QV) THROW 51031,'Attachment is outside this draft.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.PAPER_VERSION v ON v.paper_version_id=i.paper_version_id WHERE i.question_version_id=@QV AND v.frozen_at IS NOT NULL) THROW 51031,'Copy question to a new revision before removing its attachment.',1;
DELETE dbo.QUESTION_ATTACHMENT WHERE attachment_id=@AttachmentId;
UPDATE dbo.PAPER_VERSION SET content_hash=N'DRAFT' WHERE paper_version_id=@VersionId;
SELECT row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- API resolves returned storage keys to authorized download URLs; never make paper files public.
CREATE OR ALTER PROCEDURE dbo.usp_GetQuestionAttachments
@ActorId int,@VersionId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 DECLARE @Exam int=(SELECT p.course_exam_id FROM dbo.PAPER_VERSION v JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE v.paper_version_id=@VersionId);
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
SELECT a.*,f.original_name,f.mime_type,f.storage_key,f.checksum FROM dbo.QUESTION_ATTACHMENT a JOIN dbo.PAPER_ITEM i ON i.question_version_id=a.question_version_id JOIN dbo.FILE_ASSET f ON f.file_asset_id=a.file_asset_id WHERE i.paper_version_id=@VersionId ORDER BY a.display_order;
END;
GO

-- Data Cell corrects staff profile/import details; this does not grant roles or change passwords.
CREATE OR ALTER PROCEDURE dbo.usp_UpdateStaffDetails
@ActorId int,@UserId int,@FullName nvarchar(200),@Email nvarchar(256),@Designation nvarchar(100)=NULL,@Phone nvarchar(30)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DATA_CELL';
IF NULLIF(LTRIM(RTRIM(@FullName)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@Email)),N'') IS NULL THROW 51010,'Name and email required.',1;
UPDATE dbo.APP_USER SET full_name=@FullName,email=@Email,designation=@Designation,phone_number=@Phone WHERE user_id=@UserId;
IF @@ROWCOUNT=0 THROW 51010,'Staff account not found.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO

-- Director activates/deactivates staff without deleting their teaching or approval history.
CREATE OR ALTER PROCEDURE dbo.usp_SetStaffActive
@ActorId int,@UserId int,@IsActive bit
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
EXEC dbo.usp_RequireRole @ActorId,N'DIRECTOR';
IF @UserId=@ActorId THROW 51010,'Do not deactivate your own account here.',1;
UPDATE dbo.APP_USER SET is_active=@IsActive WHERE user_id=@UserId;
IF @@ROWCOUNT=0 THROW 51010,'Staff account not found.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO


-- MODULE: 10_Permissions.sql
USE [BIIT_FYP_Updated];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Run once as database owner. Bind actual SQL users in deployment, not in this file.
-- App/worker SQL identities MUST NOT be db_owner, db_datawriter or db_datareader.
-- ActorId is supplied by trusted API from authenticated claims, never request-body user input.
CREATE ROLE [biit_api_executor];
CREATE ROLE [biit_checker_executor];
CREATE ROLE [biit_auth_executor];
GO
GRANT EXECUTE ON OBJECT::dbo.usp_GetCurrentExam TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetProfile TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_UpdateProfile TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetLoginRecord TO [biit_auth_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_ChangePasswordHash TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_RegisterFile TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddDepartment TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddProgram TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddSession TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddSection TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddCourse TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddExamCycle TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_CreateUser TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GrantUserRole TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_EndUserRole TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_CreateOutline TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddOutlineTopic TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddOutlineCLO TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_PublishOutline TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddCourseOffering TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddOfferingSection TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AssignTeaching TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_ConfigureCourseExam TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SetExamCLOs TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SaveCoverage TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetTopicCoverage TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_LockCommonTopics TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SaveDatesheetDraft TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetDatesheetPreview TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_PublishDatesheet TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetDatesheetHistory TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AssignPaperSetter TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_StartPaperRevision TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SaveQuestion TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_MovePaperItem TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_ReorderPaperItems TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SavePaperInstructions TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetPaperWorkspace TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetPaperVersions TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_ReviewCLOMapping TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SubmitPaper TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_ReviewPaper TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_AddComment TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_RecordPrintExport TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_MarkPaperReceived TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_RunExactSimilarity TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetSimilarityReport TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetCheckingPayload TO [biit_checker_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SaveCLOCheckResults TO [biit_checker_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetDashboard TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_ListPapers TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_ListMyCourses TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetNotifications TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_MarkNotificationRead TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SaveWishlist TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SetWishlistState TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetWishlist TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GrantDirectorDelegation TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_RevokeDirectorDelegation TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetDelegations TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_StartImport TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_FinishImport TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_StartSemanticSimilarity TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_CompleteSemanticSimilarity TO [biit_checker_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_FailSimilarityRun TO [biit_checker_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_UpdateOutlineTopic TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_UpdateOutlineCLO TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_UpdateCourseExamSettings TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_EndTeachingAssignment TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetAcademicLookups TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetImportHistory TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_ArchivePaper TO [biit_api_executor];
GO
-- DBA deployment example (replace actual user names; no credentials are included here):
-- ALTER ROLE biit_api_executor ADD MEMBER YourApiDatabaseUser;
-- ALTER ROLE biit_checker_executor ADD MEMBER YourWorkerDatabaseUser;
-- ALTER ROLE biit_auth_executor ADD MEMBER YourAuthenticationDatabaseUser;
-- SQL ownership chaining lets procedures access tables without direct table grants.
-- Do NOT grant callers direct UPDATE/DELETE on paper/question/version tables.
GRANT EXECUTE ON OBJECT::dbo.usp_AddQuestionAttachment TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_RemoveQuestionAttachment TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_GetQuestionAttachments TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_UpdateStaffDetails TO [biit_api_executor];
GRANT EXECUTE ON OBJECT::dbo.usp_SetStaffActive TO [biit_api_executor];
GO

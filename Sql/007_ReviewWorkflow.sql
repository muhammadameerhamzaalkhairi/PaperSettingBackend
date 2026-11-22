:ON ERROR EXIT
IF DB_NAME() <> N'$(TargetDatabase)' OR
    (DB_NAME() <> N'BIIT_FYP_PaperSetting' AND DB_NAME() NOT LIKE N'BIITTest%')
    THROW 51000,'Unexpected database for Phase 7.',1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

-- Ek setter submission ke neeche asal aur Director ki frozen revisions review ho sakti hain.
IF OBJECT_ID('dbo.PAPER_REVIEW_VERSION','U') IS NULL
BEGIN
    CREATE TABLE dbo.PAPER_REVIEW_VERSION(
        submission_id int NOT NULL REFERENCES dbo.PAPER_SUBMISSION(submission_id),
        paper_version_id int NOT NULL PRIMARY KEY REFERENCES dbo.PAPER_VERSION(paper_version_id),
        UNIQUE(submission_id,paper_version_id)
    );
    INSERT dbo.PAPER_REVIEW_VERSION(submission_id,paper_version_id)
    SELECT submission_id,paper_version_id FROM dbo.PAPER_SUBMISSION;
    ALTER TABLE dbo.REVIEW ADD CONSTRAINT FK_Review_ExactVersion FOREIGN KEY(submission_id,reviewed_version_id)
        REFERENCES dbo.PAPER_REVIEW_VERSION(submission_id,paper_version_id);
    ALTER TABLE dbo.REVIEW DROP CONSTRAINT FK_Review_SubmissionVersion;
END;
GO

-- Har nayi submission ka asal version bhi isi foreign key ke zariye mehfooz rehta hai.
CREATE OR ALTER TRIGGER dbo.tr_RegisterSubmittedVersion ON dbo.PAPER_SUBMISSION AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.PAPER_REVIEW_VERSION(submission_id,paper_version_id)
    SELECT submission_id,paper_version_id FROM inserted;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RequireDirectorAuthority
    @ActorId int, @DelegationId int, @RoleId int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @DelegationId IS NULL
    BEGIN
        EXEC dbo.usp_RequireRole @ActorId,N'DIRECTOR';
        SELECT TOP(1) @RoleId=user_role_id FROM dbo.vw_ActiveUserRole
        WHERE user_id=@ActorId AND role_code='DIRECTOR' ORDER BY user_role_id;
    END
    ELSE IF NOT EXISTS (
        SELECT 1 FROM dbo.DIRECTOR_DELEGATION d WITH(UPDLOCK,HOLDLOCK)
        JOIN dbo.vw_ActiveUserRole r ON r.user_role_id=d.grantor_user_role_id AND r.role_code='DIRECTOR'
        JOIN dbo.APP_USER u ON u.user_id=d.delegate_user_id AND u.is_active=1
        WHERE d.delegation_id=@DelegationId AND d.delegate_user_id=@ActorId AND d.revoked_at IS NULL
            AND d.starts_at<=SYSUTCDATETIME() AND d.ends_at>SYSUTCDATETIME()
    ) THROW 51040,'Director delegation expired or invalid.',1;
END;
GO
-- Dono raaston par wohi marks, topics, mappings aur content snapshot check hota hai.
CREATE OR ALTER PROCEDURE dbo.usp_FreezeReviewVersion
    @VersionId int,@Exam int,@RequireAlignment bit
AS
BEGIN
SET NOCOUNT ON;
DECLARE @Content nvarchar(max);
IF NOT EXISTS(SELECT 1 FROM dbo.vw_PaperVersionSummary WHERE paper_version_id=@VersionId AND main_question_count>0 AND added_marks=required_marks_snapshot) THROW 51040,'MAIN question marks must equal required marks.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND NOT EXISTS(SELECT 1 FROM dbo.QUESTION_PART qp WHERE qp.question_version_id=i.question_version_id AND qp.part_kind=N'ITEM')) THROW 51040,'A MAIN question has no mark-bearing parts.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_PART qp ON qp.question_version_id=i.question_version_id WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND qp.part_kind=N'ITEM' AND NOT EXISTS(SELECT 1 FROM dbo.EXAM_COMMON_TOPIC WHERE course_exam_id=@Exam AND topic_id=qp.topic_id)) THROW 51040,'Question topic is outside the locked common topics.',1;
-- Each ITEM needs a mapping on itself or on its entire question. Group-only mapping does not cover children.
IF EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.QUESTION_PART qp ON qp.question_version_id=i.question_version_id WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND qp.part_kind=N'ITEM'
 AND NOT EXISTS(SELECT 1 FROM dbo.CLO_MAPPING m WHERE m.question_version_id=i.question_version_id AND (m.question_part_id IS NULL OR m.question_part_id=qp.question_part_id))) THROW 51040,'Every scored part needs a question-level or part-level CLO mapping.',1;
IF @RequireAlignment=1 AND EXISTS(SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.CLO_MAPPING m ON m.question_version_id=i.question_version_id LEFT JOIN dbo.vw_LatestCLOCheck ck ON ck.mapping_id=m.mapping_id WHERE i.paper_version_id=@VersionId AND i.placement=N'MAIN' AND (ck.check_status IS NULL OR ck.check_status<>N'ALIGNED')) THROW 51040,'All claimed CLO mappings need an ALIGNED check before submission.',1;
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

END;
GO
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
DECLARE @PaperId int,@Exam int,@SubmissionId int;
SELECT @PaperId=p.paper_id,@Exam=p.course_exam_id FROM dbo.PAPER_VERSION v JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE v.paper_version_id=@VersionId;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER WHERE paper_id=@PaperId AND workflow_status=N'DRAFT') THROW 51040,'Only a draft may be submitted.',1;
EXEC dbo.usp_FreezeReviewVersion @VersionId,@Exam,1;
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
CREATE OR ALTER PROCEDURE dbo.usp_SaveQuestion
@ActorId int,@VersionId int,@ExpectedRowVersion binary(8),@Prompt nvarchar(max),@Difficulty nvarchar(40),
@PartsJson nvarchar(max),@CloMappingsJson nvarchar(max),@PaperItemId int=NULL,@Placement nvarchar(40)=N'EXTRA',@ReviewSubmissionId int=NULL,@EditReason nvarchar(500)=NULL,@DelegationId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @PaperId int,@ExamId int,@Actual binary(8),@Frozen datetime2(0),@QId int,@OldQV int,@NewQV int,@Revision int,@IsSetter bit=0;
DECLARE @SourceVersion int=@VersionId,@SourceQuestion int,@RoleId int;
IF @ReviewSubmissionId IS NOT NULL
BEGIN
    EXEC dbo.usp_RequireDirectorAuthority @ActorId,@DelegationId,@RoleId OUTPUT;
    SELECT @PaperId=p.paper_id,@ExamId=p.course_exam_id FROM dbo.PAPER_REVIEW_VERSION link
    JOIN dbo.PAPER_VERSION v ON v.paper_version_id=link.paper_version_id
    JOIN dbo.PAPER p WITH(UPDLOCK,HOLDLOCK) ON p.paper_id=v.paper_id
    WHERE link.submission_id=@ReviewSubmissionId AND link.paper_version_id=@SourceVersion
      AND v.frozen_at IS NOT NULL AND v.row_version=@ExpectedRowVersion
      AND p.workflow_status IN ('SUBMITTED','HOD_REVIEWED');
    IF @PaperId IS NULL OR EXISTS(SELECT 1 FROM dbo.PAPER_VERSION WHERE paper_id=@PaperId AND paper_version_id>@SourceVersion)
        THROW 51040,'Review revision is stale or closed.',1;
    IF NULLIF(TRIM(@EditReason),'') IS NULL THROW 51031,'Explain the revision.',1;
    SELECT @SourceQuestion=question_version_id FROM dbo.PAPER_ITEM
    WHERE paper_version_id=@SourceVersion AND paper_item_id=@PaperItemId;
    IF @SourceQuestion IS NULL THROW 51031,'Select an item from the reviewed version.',1;
    INSERT dbo.PAPER_VERSION(paper_id,revision_number,based_on_version_id,created_by_user_id,created_at,
        revision_reason,instructions,content_hash,required_marks_snapshot,duration_minutes_snapshot)
    SELECT paper_id,revision_number+1,@SourceVersion,@ActorId,SYSUTCDATETIME(),@EditReason,instructions,
        'DRAFT',required_marks_snapshot,duration_minutes_snapshot FROM dbo.PAPER_VERSION WHERE paper_version_id=@SourceVersion;
    SET @VersionId=CONVERT(int,SCOPE_IDENTITY());
    INSERT dbo.PAPER_ITEM(paper_version_id,question_version_id,placement,sort_order,display_number)
    SELECT @VersionId,question_version_id,placement,sort_order,display_number FROM dbo.PAPER_ITEM WHERE paper_version_id=@SourceVersion;
    SELECT @PaperItemId=paper_item_id FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND question_version_id=@SourceQuestion;
    SET @IsSetter=1; -- Is branch mein sirf tasdeeq shuda Director copied item edit karta hai.
END
ELSE
BEGIN
SELECT @PaperId=v.paper_id,@ExamId=p.course_exam_id,@Actual=v.row_version,@Frozen=v.frozen_at FROM dbo.PAPER_VERSION v WITH(UPDLOCK,HOLDLOCK) JOIN dbo.PAPER p ON p.paper_id=v.paper_id WHERE v.paper_version_id=@VersionId;
IF @PaperId IS NULL OR @Frozen IS NOT NULL OR @ExpectedRowVersion IS NULL OR @Actual<>@ExpectedRowVersion THROW 51031,'Draft missing, frozen or stale. Reload.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_VERSION WHERE paper_id=@PaperId AND revision_number>(SELECT revision_number FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId)) THROW 51031,'Only latest draft is editable.',1;
EXEC dbo.usp_RequireRole @ActorId,N'TEACHER';
EXEC dbo.usp_RequireExamAccess @ActorId,@ExamId;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER_PARTICIPANT WHERE paper_id=@PaperId AND teacher_user_id=@ActorId AND left_at IS NULL) THROW 51031,'Not a paper contributor.',1;
IF EXISTS(SELECT 1 FROM dbo.PAPER_ASSIGNMENT WHERE paper_id=@PaperId AND setter_user_id=@ActorId AND ended_at IS NULL) SET @IsSetter=1;
IF @IsSetter=0 AND @Placement<>N'EXTRA' THROW 51031,'Contributors add extra questions; setter selects MAIN questions.',1;
END;
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
IF @ReviewSubmissionId IS NOT NULL
BEGIN
    EXEC dbo.usp_FreezeReviewVersion @VersionId,@ExamId,0;
    INSERT dbo.PAPER_REVIEW_VERSION(submission_id,paper_version_id) VALUES(@ReviewSubmissionId,@VersionId);
    INSERT dbo.AUDIT_EVENT(actor_user_id,delegation_id,entity_type,entity_key,action,before_data,after_data,occurred_at)
    VALUES(@ActorId,@DelegationId,'PAPER_VERSION',CONVERT(nvarchar(200),@VersionId),'DIRECTOR_EDIT',
        (SELECT @SourceVersion AS paper_version_id,@OldQV AS question_version_id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
        (SELECT @VersionId AS paper_version_id,@NewQV AS question_version_id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),SYSUTCDATETIME());
END;
SELECT @VersionId AS paper_version_id,@PaperItemId AS paper_item_id,@NewQV AS question_version_id,row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_ReviewPaper
@ActorId int,@SubmissionId int,@Stage nvarchar(40),@Decision nvarchar(40),@Feedback nvarchar(max)=NULL,@DelegationId int=NULL,@ReviewedVersionId int=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 51000, 'Call this procedure without an outer transaction.', 1;
BEGIN TRANSACTION;
BEGIN TRY
DECLARE @PaperId int,@VersionId int,@Dept int,@Status nvarchar(40),@RoleId int,@Setter int;
SELECT @PaperId=p.paper_id,@VersionId=s.paper_version_id,@Dept=o.lead_department_id,@Status=p.workflow_status FROM dbo.PAPER_SUBMISSION s JOIN dbo.PAPER_VERSION v ON v.paper_version_id=s.paper_version_id JOIN dbo.PAPER p WITH(UPDLOCK,HOLDLOCK) ON p.paper_id=v.paper_id JOIN dbo.COURSE_EXAM ce ON ce.course_exam_id=p.course_exam_id JOIN dbo.COURSE_OFFERING o ON o.offering_id=ce.offering_id WHERE s.submission_id=@SubmissionId;
IF @ReviewedVersionId IS NOT NULL SET @VersionId=@ReviewedVersionId;
IF NOT EXISTS(SELECT 1 FROM dbo.PAPER_REVIEW_VERSION WHERE submission_id=@SubmissionId AND paper_version_id=@VersionId)
    THROW 51040,'Version is outside this submission.',1;
IF @PaperId IS NULL OR EXISTS(SELECT 1 FROM dbo.PAPER_VERSION WHERE paper_id=@PaperId AND paper_version_id>@VersionId) THROW 51040,'Submission missing or not the latest version.',1;
IF @Stage NOT IN(N'HOD',N'DIRECTOR') OR @Decision NOT IN(N'APPROVED',N'CHANGES_REQUESTED') THROW 51040,'Invalid review stage/decision.',1;
IF (@Stage=N'HOD' AND @Status<>N'SUBMITTED') OR (@Stage=N'DIRECTOR' AND @Status NOT IN(N'SUBMITTED',N'HOD_REVIEWED')) THROW 51040,'Review is out of sequence or already decided.',1;
IF @Stage=N'HOD' BEGIN
 IF @DelegationId IS NOT NULL THROW 51040,'Director delegation does not authorize HOD review.',1;
 EXEC dbo.usp_RequireRole @ActorId,N'HOD',@Dept;
 SELECT TOP(1) @RoleId=user_role_id FROM dbo.vw_ActiveUserRole WHERE user_id=@ActorId AND role_code=N'HOD' AND (department_id=@Dept OR department_id IS NULL) ORDER BY user_role_id;
END
ELSE BEGIN
 EXEC dbo.usp_RequireDirectorAuthority @ActorId,@DelegationId,@RoleId OUTPUT;
END;
IF @Decision=N'CHANGES_REQUESTED' AND NULLIF(TRIM(@Feedback),N'') IS NULL THROW 51031,'Request Changes needs a reason.',1;
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
CREATE OR ALTER PROCEDURE dbo.usp_GetPaperVersions
@ActorId int,@PaperId int
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 DECLARE @Exam int=(SELECT course_exam_id FROM dbo.PAPER WHERE paper_id=@PaperId);
EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
SELECT v.*,pv.created_by_user_id,pv.based_on_version_id,pv.revision_reason,s.submission_id,s.submitted_at FROM dbo.vw_PaperVersionSummary v JOIN dbo.PAPER_VERSION pv ON pv.paper_version_id=v.paper_version_id LEFT JOIN dbo.PAPER_REVIEW_VERSION link ON link.paper_version_id=v.paper_version_id LEFT JOIN dbo.PAPER_SUBMISSION s ON s.submission_id=link.submission_id WHERE v.paper_id=@PaperId ORDER BY v.revision_number DESC;
SELECT r.* FROM dbo.REVIEW r JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id WHERE v.paper_id=@PaperId ORDER BY r.review_id;
END;
GO
COMMIT TRANSACTION;
GO

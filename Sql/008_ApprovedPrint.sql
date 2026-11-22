:ON ERROR EXIT
IF DB_NAME() <> N'$(TargetDatabase)' OR
   (DB_NAME() <> N'BIIT_FYP_PaperSetting' AND DB_NAME() NOT LIKE N'BIITTest%')
    THROW 51000,'Unexpected Phase 8 database.',1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO
-- Approval se exact revision milti hai. Client paper version ya diagram path nahi chun sakta.
CREATE OR ALTER PROCEDURE dbo.usp_GetApprovedPrintContent @ActorId int,@ApprovalReviewId int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @version int,@exam int;
    SELECT @version=v.paper_version_id,@exam=p.course_exam_id
    FROM dbo.REVIEW r JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id
    JOIN dbo.PAPER p ON p.paper_id=v.paper_id
    WHERE r.review_id=@ApprovalReviewId AND r.review_stage='DIRECTOR' AND r.decision='APPROVED'
      AND p.workflow_status IN ('APPROVED','RECEIVED') AND v.frozen_at IS NOT NULL;
    IF @version IS NULL THROW 51040,'Final approval required.',1;
    EXEC dbo.usp_RequireExamAccess @ActorId,@exam;
    SELECT e.course_code,e.course_title,e.session_name,e.exam_type,v.instructions,
        v.required_marks_snapshot AS marks,v.duration_minutes_snapshot AS minutes
    FROM dbo.vw_CourseExam e JOIN dbo.PAPER_VERSION v ON v.paper_version_id=@version WHERE e.course_exam_id=@exam;

    SELECT question_version_id,sort_order INTO #main FROM dbo.PAPER_ITEM
    WHERE paper_version_id=@version AND placement='MAIN';
    SELECT m.question_version_id,q.prompt_text FROM #main m
    JOIN dbo.QUESTION_VERSION q ON q.question_version_id=m.question_version_id ORDER BY m.sort_order;
    SELECT part.question_version_id,part.question_part_id,part.parent_part_id,part.part_label,part.part_text,part.marks
    FROM #main m JOIN dbo.QUESTION_PART part ON part.question_version_id=m.question_version_id
    ORDER BY m.sort_order,part.part_order;
    SELECT a.question_version_id,a.question_part_id,f.storage_key,f.checksum
    FROM #main m JOIN dbo.QUESTION_ATTACHMENT a ON a.question_version_id=m.question_version_id
    JOIN dbo.FILE_ASSET f ON f.file_asset_id=a.file_asset_id ORDER BY m.sort_order,a.display_order,a.attachment_id;
END;
GO
-- Private file ka metadata aur print event aik transaction mein record hotay hain.
CREATE OR ALTER PROCEDURE dbo.usp_RecordPrintExport
    @ActorId int,@ApprovalReviewId int,@FileId int=NULL,@StorageKey nvarchar(256)=NULL,@Checksum varchar(64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT<>0 THROW 51000,'Call without an outer transaction.',1;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @exam int,@export int;
        SELECT @exam=p.course_exam_id FROM dbo.REVIEW r
        JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id
        JOIN dbo.PAPER p WITH(UPDLOCK,HOLDLOCK) ON p.paper_id=v.paper_id
        WHERE r.review_id=@ApprovalReviewId AND r.review_stage='DIRECTOR' AND r.decision='APPROVED'
          AND p.workflow_status IN ('APPROVED','RECEIVED') AND v.frozen_at IS NOT NULL;
        IF @exam IS NULL THROW 51040,'Final approval required.',1;
        EXEC dbo.usp_RequireExamAccess @ActorId,@exam;
        IF @StorageKey IS NOT NULL
        BEGIN
            IF @FileId IS NOT NULL OR LEN(@StorageKey)<>32 OR @StorageKey LIKE '%[^0-9a-f]%' OR @Checksum IS NULL OR LEN(@Checksum)<>64 OR @Checksum LIKE '%[^0-9A-F]%' THROW 51040,'Invalid generated PDF.',1;
            INSERT dbo.FILE_ASSET(uploaded_by_user_id,storage_key,original_name,mime_type,checksum,created_at)
            VALUES(@ActorId,@StorageKey,CONCAT('exam-',@ApprovalReviewId,'.pdf'),'application/pdf',
                @Checksum,SYSUTCDATETIME());
            SET @FileId=CONVERT(int,SCOPE_IDENTITY());
        END;
        IF NOT EXISTS(SELECT 1 FROM dbo.FILE_ASSET WHERE file_asset_id=@FileId AND uploaded_by_user_id=@ActorId AND mime_type='application/pdf')
            THROW 51040,'Register your generated PDF first.',1;
        INSERT dbo.PRINT_EXPORT(approval_review_id,requested_by_user_id,file_asset_id,generated_at)
        VALUES(@ApprovalReviewId,@ActorId,@FileId,SYSUTCDATETIME());
        SET @export=CONVERT(int,SCOPE_IDENTITY());

        COMMIT TRANSACTION;
        SELECT @export AS print_export_id;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GetPrintExport @ActorId int,@ApprovalReviewId int,@ExportId int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @exam int;
    SELECT @exam=p.course_exam_id FROM dbo.PRINT_EXPORT e JOIN dbo.REVIEW r ON r.review_id=e.approval_review_id
    JOIN dbo.PAPER_VERSION v ON v.paper_version_id=r.reviewed_version_id JOIN dbo.PAPER p ON p.paper_id=v.paper_id
    WHERE e.print_export_id=@ExportId AND r.review_id=@ApprovalReviewId AND r.review_stage='DIRECTOR' AND r.decision='APPROVED';
    EXEC dbo.usp_RequireExamAccess @ActorId,@exam;
    SELECT f.storage_key,f.checksum FROM dbo.PRINT_EXPORT e JOIN dbo.FILE_ASSET f ON f.file_asset_id=e.file_asset_id
    WHERE e.print_export_id=@ExportId AND e.approval_review_id=@ApprovalReviewId AND f.mime_type='application/pdf';
END;
GO
GRANT EXECUTE ON dbo.usp_GetPrintExport TO biit_api_executor;
GRANT EXECUTE ON dbo.usp_GetApprovedPrintContent TO biit_api_executor;
COMMIT TRANSACTION;
GO

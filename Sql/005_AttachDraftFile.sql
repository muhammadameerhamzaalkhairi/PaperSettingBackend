IF DB_NAME() <> N'$(TargetDatabase)' OR
   (DB_NAME() <> N'BIIT_FYP_PaperSetting' AND DB_NAME() NOT LIKE N'BIITTest%')
    THROW 51000, 'Unexpected database for Phase 5.', 1;
GO
-- File registration aur attachment link aik transaction mein save hon.
CREATE OR ALTER PROCEDURE dbo.usp_AttachDraftFile
    @ActorId int, @VersionId int, @ExpectedRowVersion binary(8), @QuestionVersionId int,
    @PartId int=NULL, @DisplayOrder int, @StorageKey nvarchar(256), @OriginalName nvarchar(256),
    @MimeType nvarchar(128), @Checksum nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT<>0 THROW 51000,'Call without an outer transaction.',1;
    BEGIN TRANSACTION;
    BEGIN TRY
        EXEC dbo.usp_RequireDraftSetter @ActorId,@VersionId,@ExpectedRowVersion;
        IF NOT EXISTS (SELECT 1 FROM dbo.PAPER_ITEM WHERE paper_version_id=@VersionId AND question_version_id=@QuestionVersionId)
            THROW 51031,'Question is outside this draft.',1;
        IF EXISTS (SELECT 1 FROM dbo.PAPER_ITEM i JOIN dbo.PAPER_VERSION v ON v.paper_version_id=i.paper_version_id
                   WHERE i.question_version_id=@QuestionVersionId AND v.frozen_at IS NOT NULL)
            THROW 51003,'Create a new question revision before changing historical attachments.',1;
        IF @PartId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.QUESTION_PART WHERE question_part_id=@PartId AND question_version_id=@QuestionVersionId)
            THROW 51031,'Part is outside this question revision.',1;
        INSERT dbo.FILE_ASSET(uploaded_by_user_id,storage_key,original_name,mime_type,checksum,created_at)
        VALUES(@ActorId,@StorageKey,@OriginalName,@MimeType,@Checksum,SYSUTCDATETIME());
        DECLARE @FileId int=CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.QUESTION_ATTACHMENT(question_version_id,question_part_id,file_asset_id,display_order)
        VALUES(@QuestionVersionId,@PartId,@FileId,@DisplayOrder);
        DECLARE @AttachmentId int=CONVERT(int,SCOPE_IDENTITY());
        UPDATE dbo.PAPER_VERSION SET content_hash=N'DRAFT' WHERE paper_version_id=@VersionId;
        SELECT @AttachmentId AS attachment_id,row_version FROM dbo.PAPER_VERSION WHERE paper_version_id=@VersionId;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GRANT EXECUTE ON dbo.usp_AttachDraftFile TO biit_api_executor;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GetQuestionRevisionHistory @ActorId int,@QuestionVersionId int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Question int,@Exam int;
    SELECT @Question=q.question_id,@Exam=p.course_exam_id FROM dbo.QUESTION_VERSION v
    JOIN dbo.QUESTION q ON q.question_id=v.question_id JOIN dbo.PAPER p ON p.paper_id=q.paper_id
    WHERE v.question_version_id=@QuestionVersionId;
    EXEC dbo.usp_RequireExamAccess @ActorId,@Exam;
    SELECT question_version_id,question_id,revision_number,prompt_text,difficulty,created_at,created_by_user_id
    FROM dbo.QUESTION_VERSION WHERE question_id=@Question ORDER BY revision_number DESC;
    SELECT part.* FROM dbo.QUESTION_PART part JOIN dbo.QUESTION_VERSION v ON v.question_version_id=part.question_version_id
    WHERE v.question_id=@Question ORDER BY v.revision_number DESC,part.part_order;
    SELECT m.mapping_id,m.question_version_id,m.question_part_id,m.exam_clo_id FROM dbo.CLO_MAPPING m
    JOIN dbo.QUESTION_VERSION v ON v.question_version_id=m.question_version_id WHERE v.question_id=@Question;
END;
GO
GRANT EXECUTE ON dbo.usp_GetQuestionRevisionHistory TO biit_api_executor;
GO

IF DB_NAME() <> N'$(TargetDatabase)' OR
   (DB_NAME() <> N'BIIT_FYP_PaperSetting' AND DB_NAME() NOT LIKE N'BIITTest%')
    THROW 51000, 'Unexpected database for Phase 4.', 1;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GetCoverageWorkspace @ActorId int, @ExamId int
AS
BEGIN
    SET NOCOUNT ON;
    -- Pehle existing exam access check aur coverage ke chaar result sets.
    EXEC dbo.usp_GetTopicCoverage @ActorId, @ExamId;
    SELECT ce.scope_locked_at, p.paper_id, p.workflow_status
    FROM dbo.COURSE_EXAM ce LEFT JOIN dbo.PAPER p ON p.course_exam_id=ce.course_exam_id
    WHERE ce.course_exam_id=@ExamId;
    SELECT t.topic_id, t.topic_title FROM dbo.EXAM_COMMON_TOPIC ct
    JOIN dbo.TOPIC t ON t.topic_id=ct.topic_id
    WHERE ct.course_exam_id=@ExamId ORDER BY t.sort_order,t.topic_id;
    SELECT a.assignment_id,a.paper_id,a.setter_user_id,u.full_name,a.assigned_by_user_role_id,
           a.assigned_at,a.due_at,a.ended_at
    FROM dbo.PAPER p JOIN dbo.PAPER_ASSIGNMENT a ON a.paper_id=p.paper_id
    JOIN dbo.APP_USER u ON u.user_id=a.setter_user_id
    WHERE p.course_exam_id=@ExamId ORDER BY a.assignment_id DESC;
END;
GO
GRANT EXECUTE ON dbo.usp_GetCoverageWorkspace TO biit_api_executor;
GO

-- Run with sqlcmd -b -d <database> -v TargetDatabase=<database>.
IF DB_NAME() <> N'$(TargetDatabase)' OR
   (DB_NAME() <> N'BIIT_FYP_PaperSetting' AND DB_NAME() NOT LIKE N'BIITTest%')
    THROW 51000, 'Unexpected database for Phase 3.', 1;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GetDatesheetCourses
    @ActorId int, @ExamCycleId int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.usp_RequireRole @ActorId, N'DATA_CELL';
    IF NOT EXISTS (SELECT 1 FROM dbo.EXAM_CYCLE WHERE exam_cycle_id = @ExamCycleId)
        THROW 51021, 'Exam cycle does not exist.', 1;
    SELECT course_exam_id, course_code, course_title FROM dbo.vw_CourseExam
    WHERE exam_cycle_id = @ExamCycleId ORDER BY course_code;
END;
GO
GRANT EXECUTE ON dbo.usp_GetDatesheetCourses TO biit_api_executor;
GO

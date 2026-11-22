-- BIIT Paper Setting | Fresh installation | 2026-11-21
-- Execute this entire file ONCE in SSMS. BIIT_FYP_PaperSetting must not already exist.
-- Creates the database, 52 tables, constraints, indexes and essential seed rows.
-- No DROP statements, IF/ELSE, procedures, triggers or dummy users/data.
-- Workflow: setter submits -> HOD review -> Director final approval -> Data Cell receipt.
-- HOD APPROVED means review passed, not permission to print.
-- Corrections at either review stage use CHANGES_REQUESTED; resubmit a NEW version.
-- Database rules needing multi-row/business validation are documented at the end.

USE [master];
GO
CREATE DATABASE [BIIT_FYP_PaperSetting];
GO
USE [BIIT_FYP_PaperSetting];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

CREATE TABLE [dbo].[DEPARTMENT] (
    [department_id] int IDENTITY(1,1) NOT NULL,
    [department_code] nvarchar(40) NOT NULL,
    [department_name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_DEPARTMENT] PRIMARY KEY ([department_id]),
    CONSTRAINT [UQ_DEPARTMENT_1] UNIQUE ([department_code])
);

CREATE TABLE [dbo].[APP_USER] (
    [user_id] int IDENTITY(1,1) NOT NULL,
    [staff_number] nvarchar(200) NOT NULL,
    [full_name] nvarchar(200) NOT NULL,
    [email] nvarchar(256) NOT NULL,
    [password_hash] nvarchar(256) NOT NULL,
    [home_department_id] int NULL,
    [phone_number] nvarchar(30) NULL,
    [designation] nvarchar(100) NULL,
    [profile_photo_file_asset_id] int NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_APP_USER_is_active] DEFAULT (1),
    CONSTRAINT [PK_APP_USER] PRIMARY KEY ([user_id]),
    CONSTRAINT [UQ_APP_USER_1] UNIQUE ([staff_number]),
    CONSTRAINT [UQ_APP_USER_2] UNIQUE ([email])
);

CREATE TABLE [dbo].[ROLE] (
    [role_id] int IDENTITY(1,1) NOT NULL,
    [role_code] nvarchar(40) NOT NULL,
    CONSTRAINT [PK_ROLE] PRIMARY KEY ([role_id]),
    CONSTRAINT [UQ_ROLE_1] UNIQUE ([role_code]),
    CONSTRAINT [CK_ROLE_1] CHECK ([role_code] IN (N'TEACHER',N'HOD',N'DIRECTOR',N'DATA_CELL'))
);

CREATE TABLE [dbo].[USER_ROLE] (
    [user_role_id] int IDENTITY(1,1) NOT NULL,
    [user_id] int NOT NULL,
    [role_id] int NOT NULL,
    [department_id] int NULL,
    [valid_from] datetime2(0) NOT NULL,
    [valid_until] datetime2(0) NULL,
    CONSTRAINT [PK_USER_ROLE] PRIMARY KEY ([user_role_id]),
    CONSTRAINT [UQ_USER_ROLE_1] UNIQUE ([user_id], [role_id], [department_id], [valid_from]),
    CONSTRAINT [CK_USER_ROLE_1] CHECK ([valid_until] IS NULL OR [valid_until] > [valid_from])
);

CREATE TABLE [dbo].[DIRECTOR_DELEGATION] (
    [delegation_id] int IDENTITY(1,1) NOT NULL,
    [grantor_user_role_id] int NOT NULL,
    [delegate_user_id] int NOT NULL,
    [starts_at] datetime2(0) NOT NULL,
    [ends_at] datetime2(0) NOT NULL,
    [revoked_at] datetime2(0) NULL,
    [revoked_by_user_id] int NULL,
    [reason] nvarchar(500) NULL,
    CONSTRAINT [PK_DIRECTOR_DELEGATION] PRIMARY KEY ([delegation_id]),
    CONSTRAINT [CK_DIRECTOR_DELEGATION_1] CHECK ([ends_at] > [starts_at]),
    CONSTRAINT [CK_DIRECTOR_DELEGATION_2] CHECK (([revoked_at] IS NULL AND [revoked_by_user_id] IS NULL) OR ([revoked_at] IS NOT NULL AND [revoked_by_user_id] IS NOT NULL))
);

CREATE TABLE [dbo].[ACADEMIC_SESSION] (
    [session_id] int IDENTITY(1,1) NOT NULL,
    [session_name] nvarchar(200) NOT NULL,
    [starts_on] date NOT NULL,
    [ends_on] date NOT NULL,
    CONSTRAINT [PK_ACADEMIC_SESSION] PRIMARY KEY ([session_id]),
    CONSTRAINT [UQ_ACADEMIC_SESSION_1] UNIQUE ([session_name]),
    CONSTRAINT [CK_ACADEMIC_SESSION_1] CHECK ([ends_on] > [starts_on])
);

CREATE TABLE [dbo].[PROGRAM] (
    [program_id] int IDENTITY(1,1) NOT NULL,
    [department_id] int NOT NULL,
    [program_code] nvarchar(40) NOT NULL,
    [program_name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_PROGRAM] PRIMARY KEY ([program_id]),
    CONSTRAINT [UQ_PROGRAM_1] UNIQUE ([program_code])
);

CREATE TABLE [dbo].[SECTION] (
    [section_id] int IDENTITY(1,1) NOT NULL,
    [program_id] int NOT NULL,
    [session_id] int NOT NULL,
    [section_name] nvarchar(200) NOT NULL,
    [semester_number] int NOT NULL,
    CONSTRAINT [PK_SECTION] PRIMARY KEY ([section_id]),
    CONSTRAINT [UQ_SECTION_1] UNIQUE ([program_id], [session_id], [semester_number], [section_name]),
    CONSTRAINT [CK_SECTION_1] CHECK ([semester_number] BETWEEN 1 AND 12)
);

CREATE TABLE [dbo].[COURSE] (
    [course_id] int IDENTITY(1,1) NOT NULL,
    [owner_department_id] int NOT NULL,
    [course_code] nvarchar(40) NOT NULL,
    [course_title] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_COURSE] PRIMARY KEY ([course_id]),
    CONSTRAINT [UQ_COURSE_1] UNIQUE ([course_code])
);

CREATE TABLE [dbo].[COURSE_OUTLINE] (
    [outline_id] int IDENTITY(1,1) NOT NULL,
    [course_id] int NOT NULL,
    [version_number] int NOT NULL,
    [published_at] datetime2(0) NULL,
    CONSTRAINT [PK_COURSE_OUTLINE] PRIMARY KEY ([outline_id]),
    CONSTRAINT [UQ_COURSE_OUTLINE_1] UNIQUE ([course_id], [version_number]),
    CONSTRAINT [CK_COURSE_OUTLINE_1] CHECK ([version_number] > 0)
);

CREATE TABLE [dbo].[COURSE_OFFERING] (
    [offering_id] int IDENTITY(1,1) NOT NULL,
    [course_id] int NOT NULL,
    [outline_id] int NOT NULL,
    [session_id] int NOT NULL,
    [lead_department_id] int NOT NULL,
    CONSTRAINT [PK_COURSE_OFFERING] PRIMARY KEY ([offering_id]),
    CONSTRAINT [UQ_COURSE_OFFERING_1] UNIQUE ([course_id], [session_id])
);

CREATE TABLE [dbo].[OFFERING_SECTION] (
    [offering_section_id] int IDENTITY(1,1) NOT NULL,
    [offering_id] int NOT NULL,
    [section_id] int NOT NULL,
    CONSTRAINT [PK_OFFERING_SECTION] PRIMARY KEY ([offering_section_id]),
    CONSTRAINT [UQ_OFFERING_SECTION_1] UNIQUE ([offering_id], [section_id])
);

CREATE TABLE [dbo].[TEACHING_ASSIGNMENT] (
    [teaching_assignment_id] int IDENTITY(1,1) NOT NULL,
    [offering_section_id] int NOT NULL,
    [teacher_user_id] int NOT NULL,
    [starts_at] datetime2(0) NOT NULL,
    [ends_at] datetime2(0) NULL,
    CONSTRAINT [PK_TEACHING_ASSIGNMENT] PRIMARY KEY ([teaching_assignment_id]),
    CONSTRAINT [CK_TEACHING_ASSIGNMENT_1] CHECK ([ends_at] IS NULL OR [ends_at] > [starts_at])
);

CREATE TABLE [dbo].[TOPIC] (
    [topic_id] int IDENTITY(1,1) NOT NULL,
    [outline_id] int NOT NULL,
    [topic_title] nvarchar(200) NOT NULL,
    [week_number] int NULL,
    [parent_topic_id] int NULL,
    [sort_order] int NOT NULL CONSTRAINT [DF_TOPIC_sort_order] DEFAULT (1),
    CONSTRAINT [UQ_TOPIC_Outline] UNIQUE ([topic_id], [outline_id]),
    CONSTRAINT [CK_TOPIC_Parent] CHECK ([parent_topic_id] IS NULL OR [parent_topic_id] <> [topic_id]),
    CONSTRAINT [CK_TOPIC_Order] CHECK ([sort_order] > 0),
    CONSTRAINT [CK_TOPIC_Week] CHECK ([week_number] IS NULL OR [week_number] > 0),
    CONSTRAINT [PK_TOPIC] PRIMARY KEY ([topic_id]),
    CONSTRAINT [UQ_TOPIC_1] UNIQUE ([outline_id], [parent_topic_id], [topic_title])
);

CREATE TABLE [dbo].[CLO] (
    [clo_id] int IDENTITY(1,1) NOT NULL,
    [outline_id] int NOT NULL,
    [clo_code] nvarchar(40) NOT NULL,
    [clo_statement] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CLO] PRIMARY KEY ([clo_id]),
    CONSTRAINT [UQ_CLO_1] UNIQUE ([outline_id], [clo_code])
);

CREATE TABLE [dbo].[TOPIC_CLO] (
    [topic_id] int NOT NULL,
    [clo_id] int NOT NULL,
    CONSTRAINT [PK_TOPIC_CLO] PRIMARY KEY ([topic_id], [clo_id])
);

CREATE TABLE [dbo].[EXAM_CYCLE] (
    [exam_cycle_id] int IDENTITY(1,1) NOT NULL,
    [session_id] int NOT NULL,
    [exam_type] nvarchar(40) NOT NULL,
    CONSTRAINT [PK_EXAM_CYCLE] PRIMARY KEY ([exam_cycle_id]),
    CONSTRAINT [UQ_EXAM_CYCLE_1] UNIQUE ([session_id], [exam_type])
);

CREATE TABLE [dbo].[COURSE_EXAM] (
    [course_exam_id] int IDENTITY(1,1) NOT NULL,
    [offering_id] int NOT NULL,
    [exam_cycle_id] int NOT NULL,
    [required_marks] decimal(10,2) NOT NULL,
    [duration_minutes] int NOT NULL,
    [configured_by_user_id] int NOT NULL,
    [scope_locked_at] datetime2(0) NULL,
    CONSTRAINT [PK_COURSE_EXAM] PRIMARY KEY ([course_exam_id]),
    CONSTRAINT [UQ_COURSE_EXAM_Cycle] UNIQUE ([course_exam_id], [exam_cycle_id]),
    CONSTRAINT [UQ_COURSE_EXAM_1] UNIQUE ([offering_id], [exam_cycle_id]),
    CONSTRAINT [CK_COURSE_EXAM_1] CHECK ([required_marks] > 0),
    CONSTRAINT [CK_COURSE_EXAM_2] CHECK ([duration_minutes] > 0)
);

CREATE TABLE [dbo].[EXAM_CLO] (
    [exam_clo_id] int IDENTITY(1,1) NOT NULL,
    [course_exam_id] int NOT NULL,
    [clo_id] int NOT NULL,
    CONSTRAINT [PK_EXAM_CLO] PRIMARY KEY ([exam_clo_id]),
    CONSTRAINT [UQ_EXAM_CLO_1] UNIQUE ([course_exam_id], [clo_id])
);

-- One row per required teaching assignment and exam, even when zero topics are covered.
-- DRAFT (or no header yet) means pending. SUBMITTED with zero covered rows is valid.
CREATE TABLE [dbo].[TOPIC_COVERAGE_SUBMISSION] (
    [coverage_submission_id] int IDENTITY(1,1) NOT NULL,
    [teaching_assignment_id] int NOT NULL,
    [course_exam_id] int NOT NULL,
    [submission_status] nvarchar(20) NOT NULL CONSTRAINT [DF_CoverageSubmission_Status] DEFAULT (N'DRAFT'),
    [submitted_by_user_id] int NULL,
    [submitted_at] datetime2(0) NULL,
    [row_version] rowversion NOT NULL,
    CONSTRAINT [PK_TOPIC_COVERAGE_SUBMISSION] PRIMARY KEY ([coverage_submission_id]),
    CONSTRAINT [UQ_CoverageSubmission_AssignmentExam] UNIQUE ([teaching_assignment_id], [course_exam_id]),
    CONSTRAINT [CK_CoverageSubmission_Status] CHECK (
        ([submission_status]=N'DRAFT' AND [submitted_by_user_id] IS NULL AND [submitted_at] IS NULL)
        OR ([submission_status]=N'SUBMITTED' AND [submitted_by_user_id] IS NOT NULL AND [submitted_at] IS NOT NULL)
    )
);

CREATE TABLE [dbo].[TOPIC_COVERAGE] (
    [coverage_id] int IDENTITY(1,1) NOT NULL,
    [teaching_assignment_id] int NOT NULL,
    [course_exam_id] int NOT NULL,
    [topic_id] int NOT NULL,
    [coverage_status] nvarchar(40) NOT NULL,
    CONSTRAINT [PK_TOPIC_COVERAGE] PRIMARY KEY ([coverage_id]),
    CONSTRAINT [UQ_TOPIC_COVERAGE_1] UNIQUE ([teaching_assignment_id], [course_exam_id], [topic_id]),
    CONSTRAINT [CK_TOPIC_COVERAGE_1] CHECK ([coverage_status] IN (N'NOT_STARTED',N'IN_PROGRESS',N'COVERED'))
);

CREATE TABLE [dbo].[PAPER] (
    [paper_id] int IDENTITY(1,1) NOT NULL,
    [course_exam_id] int NOT NULL,
    [workflow_status] nvarchar(40) NOT NULL,
    [archived_at] datetime2(0) NULL,
    [deleted_at] datetime2(0) NULL,
    [deleted_by_user_id] int NULL,
    CONSTRAINT [PK_PAPER] PRIMARY KEY ([paper_id]),
    CONSTRAINT [UQ_PAPER_1] UNIQUE ([course_exam_id]),
    CONSTRAINT [CK_PAPER_1] CHECK ([workflow_status] IN (N'ASSIGNED',N'DRAFT',N'SUBMITTED',N'HOD_REVIEWED',N'CHANGES_REQUESTED',N'APPROVED',N'RECEIVED',N'ARCHIVED')),
    CONSTRAINT [CK_PAPER_2] CHECK (([deleted_at] IS NULL AND [deleted_by_user_id] IS NULL) OR ([deleted_at] IS NOT NULL AND [deleted_by_user_id] IS NOT NULL))
);

CREATE TABLE [dbo].[PAPER_ASSIGNMENT] (
    [assignment_id] int IDENTITY(1,1) NOT NULL,
    [paper_id] int NOT NULL,
    [setter_user_id] int NOT NULL,
    [assigned_by_user_role_id] int NOT NULL,
    [assigned_at] datetime2(0) NOT NULL,
    [due_at] datetime2(0) NOT NULL,
    [ended_at] datetime2(0) NULL,
    CONSTRAINT [PK_PAPER_ASSIGNMENT] PRIMARY KEY ([assignment_id]),
    CONSTRAINT [CK_PAPER_ASSIGNMENT_1] CHECK ([due_at] >= [assigned_at]),
    CONSTRAINT [CK_PAPER_ASSIGNMENT_2] CHECK ([ended_at] IS NULL OR [ended_at] >= [assigned_at])
);

CREATE TABLE [dbo].[PAPER_PARTICIPANT] (
    [paper_id] int NOT NULL,
    [teacher_user_id] int NOT NULL,
    [joined_at] datetime2(0) NOT NULL,
    [left_at] datetime2(0) NULL,
    CONSTRAINT [PK_PAPER_PARTICIPANT] PRIMARY KEY ([paper_id], [teacher_user_id]),
    CONSTRAINT [CK_PAPER_PARTICIPANT_1] CHECK ([left_at] IS NULL OR [left_at] >= [joined_at])
);

CREATE TABLE [dbo].[PAPER_VERSION] (
    [paper_version_id] int IDENTITY(1,1) NOT NULL,
    [paper_id] int NOT NULL,
    [revision_number] int NOT NULL,
    [based_on_version_id] int NULL,
    [copied_from_version_id] int NULL,
    [created_by_user_id] int NOT NULL,
    [created_at] datetime2(0) NOT NULL,
    [revision_reason] nvarchar(500) NOT NULL,
    [instructions] nvarchar(max) NULL,
    [content_hash] nvarchar(256) NOT NULL,
    [required_marks_snapshot] decimal(10,2) NOT NULL,
    [duration_minutes_snapshot] int NOT NULL,
    [frozen_at] datetime2(0) NULL,
    [row_version] rowversion NOT NULL,
    CONSTRAINT [CK_PaperVersion_Marks] CHECK ([required_marks_snapshot] > 0),
    CONSTRAINT [CK_PaperVersion_Duration] CHECK ([duration_minutes_snapshot] > 0),
    CONSTRAINT [CK_PaperVersion_Frozen] CHECK ([frozen_at] IS NULL OR [frozen_at] >= [created_at]),
    CONSTRAINT [PK_PAPER_VERSION] PRIMARY KEY ([paper_version_id]),
    CONSTRAINT [UQ_PAPER_VERSION_1] UNIQUE ([paper_id], [revision_number]),
    CONSTRAINT [CK_PAPER_VERSION_1] CHECK ([revision_number] > 0)
);

CREATE TABLE [dbo].[PAPER_SUBMISSION] (
    [submission_id] int IDENTITY(1,1) NOT NULL,
    [paper_version_id] int NOT NULL,
    [submission_number] int NOT NULL,
    [submitted_by_user_id] int NOT NULL,
    [submitted_at] datetime2(0) NOT NULL,
    CONSTRAINT [PK_PAPER_SUBMISSION] PRIMARY KEY ([submission_id]),
    CONSTRAINT [UQ_PAPER_SUBMISSION_1] UNIQUE ([paper_version_id]),
    CONSTRAINT [CK_PAPER_SUBMISSION_1] CHECK ([submission_number] > 0)
);

CREATE TABLE [dbo].[QUESTION] (
    [question_id] int IDENTITY(1,1) NOT NULL,
    [paper_id] int NOT NULL,
    [author_user_id] int NOT NULL,
    [source_paper_item_id] int NULL,
    [created_at] datetime2(0) NOT NULL,
    [deleted_at] datetime2(0) NULL,
    [deleted_by_user_id] int NULL,
    CONSTRAINT [PK_QUESTION] PRIMARY KEY ([question_id]),
    CONSTRAINT [CK_QUESTION_1] CHECK (([deleted_at] IS NULL AND [deleted_by_user_id] IS NULL) OR ([deleted_at] IS NOT NULL AND [deleted_by_user_id] IS NOT NULL))
);

CREATE TABLE [dbo].[QUESTION_VERSION] (
    [question_version_id] int IDENTITY(1,1) NOT NULL,
    [question_id] int NOT NULL,
    [revision_number] int NOT NULL,
    [prompt_text] nvarchar(max) NOT NULL,
    [difficulty] nvarchar(40) NOT NULL,
    [created_by_user_id] int NOT NULL,
    [created_at] datetime2(0) NOT NULL,
    CONSTRAINT [PK_QUESTION_VERSION] PRIMARY KEY ([question_version_id]),
    CONSTRAINT [UQ_QUESTION_VERSION_1] UNIQUE ([question_id], [revision_number]),
    CONSTRAINT [CK_QUESTION_VERSION_1] CHECK ([difficulty] IN (N'EASY',N'MEDIUM',N'HARD')),
    CONSTRAINT [CK_QUESTION_VERSION_2] CHECK ([revision_number] > 0)
);

CREATE TABLE [dbo].[QUESTION_PART] (
    [question_part_id] int IDENTITY(1,1) NOT NULL,
    [question_version_id] int NOT NULL,
    [parent_part_id] int NULL,
    [part_kind] nvarchar(10) NOT NULL CONSTRAINT [DF_QuestionPart_Kind] DEFAULT (N'ITEM'),
    [difficulty] nvarchar(40) NULL,
    [part_label] nvarchar(200) NOT NULL,
    [part_order] int NOT NULL,
    [part_text] nvarchar(max) NOT NULL,
    [topic_id] int NULL,
    [marks] decimal(10,2) NULL,
    CONSTRAINT [PK_QUESTION_PART] PRIMARY KEY ([question_part_id]),
    CONSTRAINT [UQ_QUESTION_PART_1] UNIQUE ([question_version_id], [parent_part_id], [part_order]),
    CONSTRAINT [CK_QUESTION_PART_1] CHECK (
        ([part_kind]=N'ITEM' AND [marks] IS NOT NULL AND [marks] > 0
         AND [topic_id] IS NOT NULL AND [difficulty] IS NOT NULL AND [difficulty] IN (N'EASY',N'MEDIUM',N'HARD'))
        OR ([part_kind]=N'GROUP' AND [marks] IS NULL AND [topic_id] IS NULL AND [difficulty] IS NULL)
    ),
    CONSTRAINT [CK_QuestionPart_Parent] CHECK ([parent_part_id] IS NULL OR [parent_part_id] <> [question_part_id]),
    CONSTRAINT [CK_QUESTION_PART_2] CHECK ([part_order] > 0)
);

CREATE TABLE dbo.CLO_MAPPING (
 mapping_id int IDENTITY(1,1) PRIMARY KEY,
 question_version_id int NOT NULL,
 question_part_id int NULL,
 exam_clo_id int NOT NULL,
 mapped_at datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
 UNIQUE (question_version_id, question_part_id, exam_clo_id)
);

CREATE TABLE [dbo].[PAPER_ITEM] (
    [paper_item_id] int IDENTITY(1,1) NOT NULL,
    [paper_version_id] int NOT NULL,
    [question_version_id] int NOT NULL,
    [placement] nvarchar(40) NOT NULL,
    [sort_order] int NOT NULL,
    [display_number] nvarchar(200) NULL,
    CONSTRAINT [PK_PAPER_ITEM] PRIMARY KEY ([paper_item_id]),
    CONSTRAINT [UQ_PAPER_ITEM_1] UNIQUE ([paper_version_id], [placement], [sort_order]),
    CONSTRAINT [UQ_PAPER_ITEM_2] UNIQUE ([paper_version_id], [question_version_id]),
    CONSTRAINT [CK_PAPER_ITEM_1] CHECK ([placement] IN (N'MAIN',N'EXTRA')),
    CONSTRAINT [CK_PAPER_ITEM_2] CHECK ([sort_order] > 0)
);

CREATE TABLE [dbo].[REVIEW] (
    [review_id] int IDENTITY(1,1) NOT NULL,
    [submission_id] int NOT NULL,
    [reviewed_version_id] int NOT NULL,
    [reviewer_user_id] int NOT NULL,
    [reviewer_user_role_id] int NULL,
    [delegation_id] int NULL,
    [review_stage] nvarchar(40) NOT NULL,
    [decision] nvarchar(40) NOT NULL,
    [feedback] nvarchar(max) NULL,
    [reviewed_at] datetime2(0) NOT NULL,
    CONSTRAINT [PK_REVIEW] PRIMARY KEY ([review_id]),
    CONSTRAINT [UQ_Review_SubmissionStage] UNIQUE ([submission_id], [review_stage]),
    CONSTRAINT [CK_Review_ChangeFeedback] CHECK ([decision] <> N'CHANGES_REQUESTED'
        OR ([feedback] IS NOT NULL AND LEN(LTRIM(RTRIM([feedback]))) > 0)),
    CONSTRAINT [CK_Review_DelegationStage] CHECK ([delegation_id] IS NULL OR [review_stage]=N'DIRECTOR'),
    CONSTRAINT [CK_REVIEW_1] CHECK (([review_stage]=N'HOD' AND [decision] IN (N'APPROVED',N'CHANGES_REQUESTED')) OR ([review_stage]=N'DIRECTOR' AND [decision] IN (N'APPROVED',N'CHANGES_REQUESTED'))),
    CONSTRAINT [CK_REVIEW_2] CHECK (([reviewer_user_role_id] IS NOT NULL AND [delegation_id] IS NULL) OR ([reviewer_user_role_id] IS NULL AND [delegation_id] IS NOT NULL))
);

CREATE TABLE [dbo].[COMMENT] (
    [comment_id] int IDENTITY(1,1) NOT NULL,
    [paper_version_id] int NOT NULL,
    [paper_item_id] int NULL,
    [parent_comment_id] int NULL,
    [author_user_id] int NOT NULL,
    [target_type] nvarchar(40) NOT NULL,
    [comment_text] nvarchar(max) NOT NULL,
    [created_at] datetime2(0) NOT NULL,
    [acknowledged_by_user_id] int NULL,
    [acknowledged_at] datetime2(0) NULL,
    CONSTRAINT [PK_COMMENT] PRIMARY KEY ([comment_id]),
    CONSTRAINT [CK_COMMENT_1] CHECK ([target_type] IN (N'PAPER',N'QUESTION')),
    CONSTRAINT [CK_COMMENT_2] CHECK (([target_type]=N'PAPER' AND [paper_item_id] IS NULL) OR ([target_type]=N'QUESTION' AND [paper_item_id] IS NOT NULL))
);

CREATE TABLE [dbo].[PRINT_EXPORT] (
    [print_export_id] int IDENTITY(1,1) NOT NULL,
    [approval_review_id] int NOT NULL,
    [requested_by_user_id] int NOT NULL,
    [file_asset_id] int NOT NULL,
    [generated_at] datetime2(0) NOT NULL,
    CONSTRAINT [PK_PRINT_EXPORT] PRIMARY KEY ([print_export_id])
);

CREATE TABLE [dbo].[PHYSICAL_RECEIPT] (
    [receipt_id] int IDENTITY(1,1) NOT NULL,
    [approval_review_id] int NOT NULL,
    [received_by_user_id] int NOT NULL,
    [delivered_by_user_id] int NOT NULL,
    [received_at] datetime2(0) NOT NULL,
    [seal_status] nvarchar(40) NOT NULL,
    [remarks] nvarchar(max) NULL,
    CONSTRAINT [PK_PHYSICAL_RECEIPT] PRIMARY KEY ([receipt_id]),
    CONSTRAINT [UQ_PHYSICAL_RECEIPT_1] UNIQUE ([approval_review_id]),
    CONSTRAINT [CK_PHYSICAL_RECEIPT_1] CHECK ([seal_status]=N'SEALED')
);

CREATE TABLE [dbo].[SIMILARITY_RUN] (
    [similarity_run_id] int IDENTITY(1,1) NOT NULL,
    [paper_version_id] int NOT NULL,
    [match_mode] nvarchar(20) NOT NULL CONSTRAINT [DF_SimilarityRun_Mode] DEFAULT (N'EXACT'),
    CONSTRAINT [CK_SimilarityRun_Mode] CHECK ([match_mode] IN (N'EXACT',N'SEMANTIC')),
    [algorithm_version] nvarchar(200) NOT NULL,
    [corpus_scope] nvarchar(max) NOT NULL,
    [corpus_hash] nvarchar(256) NOT NULL,
    [aggregation_method] nvarchar(200) NOT NULL,
    [run_status] nvarchar(40) NOT NULL,
    [overall_percent] decimal(10,2) NULL,
    [computed_at] datetime2(0) NULL,
    CONSTRAINT [PK_SIMILARITY_RUN] PRIMARY KEY ([similarity_run_id]),
    CONSTRAINT [CK_SIMILARITY_RUN_1] CHECK ([run_status] IN (N'PENDING',N'COMPLETED',N'FAILED')),
    CONSTRAINT [CK_SIMILARITY_RUN_2] CHECK ([overall_percent] IS NULL OR [overall_percent] BETWEEN 0 AND 100)
);

CREATE TABLE [dbo].[QUESTION_MATCH] (
    [question_match_id] int IDENTITY(1,1) NOT NULL,
    [similarity_run_id] int NOT NULL,
    [current_item_id] int NOT NULL,
    [source_item_id] int NOT NULL,
    [similarity_percent] decimal(10,2) NOT NULL,
    CONSTRAINT [PK_QUESTION_MATCH] PRIMARY KEY ([question_match_id]),
    CONSTRAINT [UQ_QUESTION_MATCH_1] UNIQUE ([similarity_run_id], [current_item_id], [source_item_id]),
    CONSTRAINT [CK_QUESTION_MATCH_1] CHECK ([similarity_percent] BETWEEN 0 AND 100),
    CONSTRAINT [CK_QUESTION_MATCH_2] CHECK ([current_item_id] <> [source_item_id])
);

CREATE TABLE [dbo].[PAPER_MATCH] (
    [paper_match_id] int IDENTITY(1,1) NOT NULL,
    [similarity_run_id] int NOT NULL,
    [source_paper_version_id] int NOT NULL,
    [similarity_percent] decimal(10,2) NOT NULL,
    CONSTRAINT [PK_PAPER_MATCH] PRIMARY KEY ([paper_match_id]),
    CONSTRAINT [UQ_PAPER_MATCH_1] UNIQUE ([similarity_run_id], [source_paper_version_id]),
    CONSTRAINT [CK_PAPER_MATCH_1] CHECK ([similarity_percent] BETWEEN 0 AND 100)
);

CREATE TABLE [dbo].[FILE_ASSET] (
    [file_asset_id] int IDENTITY(1,1) NOT NULL,
    [uploaded_by_user_id] int NOT NULL,
    [storage_key] nvarchar(256) NOT NULL,
    [original_name] nvarchar(256) NOT NULL,
    [mime_type] nvarchar(128) NOT NULL,
    [checksum] nvarchar(256) NOT NULL,
    [created_at] datetime2(0) NOT NULL,
    CONSTRAINT [PK_FILE_ASSET] PRIMARY KEY ([file_asset_id]),
    CONSTRAINT [UQ_FILE_ASSET_1] UNIQUE ([storage_key])
);

CREATE TABLE [dbo].[QUESTION_ATTACHMENT] (
    [attachment_id] int IDENTITY(1,1) NOT NULL,
    [question_version_id] int NOT NULL,
    [question_part_id] int NULL,
    [file_asset_id] int NOT NULL,
    [display_order] int NOT NULL,
    CONSTRAINT [PK_QUESTION_ATTACHMENT] PRIMARY KEY ([attachment_id])
);

CREATE TABLE [dbo].[IMPORT_BATCH] (
    [import_batch_id] int IDENTITY(1,1) NOT NULL,
    [file_asset_id] int NOT NULL,
    [imported_by_user_id] int NOT NULL,
    [import_status] nvarchar(40) NOT NULL,
    [rows_added] int NOT NULL,
    [rows_updated] int NOT NULL,
    [rows_failed] int NOT NULL,
    [started_at] datetime2(0) NOT NULL,
    [completed_at] datetime2(0) NULL,
    CONSTRAINT [PK_IMPORT_BATCH] PRIMARY KEY ([import_batch_id]),
    CONSTRAINT [CK_IMPORT_BATCH_1] CHECK ([import_status] IN (N'PENDING',N'COMPLETED',N'PARTIAL',N'FAILED')),
    CONSTRAINT [CK_IMPORT_BATCH_2] CHECK ([rows_added] >= 0 AND [rows_updated] >= 0 AND [rows_failed] >= 0)
);

CREATE TABLE [dbo].[IMPORT_ERROR] (
    [import_error_id] int IDENTITY(1,1) NOT NULL,
    [import_batch_id] int NOT NULL,
    [sheet_name] nvarchar(200) NOT NULL,
    [row_number] int NOT NULL,
    [column_name] nvarchar(200) NULL,
    [error_message] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_IMPORT_ERROR] PRIMARY KEY ([import_error_id]),
    CONSTRAINT [CK_IMPORT_ERROR_1] CHECK ([row_number] > 0)
);

CREATE TABLE [dbo].[WISHLIST] (
    [wishlist_id] int IDENTITY(1,1) NOT NULL,
    [owner_user_id] int NOT NULL,
    [course_id] int NULL,
    [paper_id] int NULL,
    [title] nvarchar(500) NOT NULL,
    [note_text] nvarchar(max) NULL,
    [is_done] bit NOT NULL CONSTRAINT [DF_WISHLIST_is_done] DEFAULT (0),
    [due_at] datetime2(0) NULL,
    [deleted_at] datetime2(0) NULL,
    [deleted_by_user_id] int NULL,
    CONSTRAINT [PK_WISHLIST] PRIMARY KEY ([wishlist_id]),
    CONSTRAINT [CK_WISHLIST_1] CHECK (([deleted_at] IS NULL AND [deleted_by_user_id] IS NULL) OR ([deleted_at] IS NOT NULL AND [deleted_by_user_id] IS NOT NULL))
);

CREATE TABLE [dbo].[NOTIFICATION] (
    [notification_id] int IDENTITY(1,1) NOT NULL,
    [recipient_user_id] int NOT NULL,
    [paper_id] int NULL,
    [delegation_id] int NULL,
    [event_type] nvarchar(40) NOT NULL,
    [message_text] nvarchar(max) NOT NULL,
    [created_at] datetime2(0) NOT NULL,
    [read_at] datetime2(0) NULL,
    CONSTRAINT [PK_NOTIFICATION] PRIMARY KEY ([notification_id])
);

CREATE TABLE [dbo].[AUDIT_EVENT] (
    [audit_event_id] int IDENTITY(1,1) NOT NULL,
    [actor_user_id] int NULL,
    [delegation_id] int NULL,
    [entity_type] nvarchar(40) NOT NULL,
    [entity_key] nvarchar(200) NOT NULL,
    [action] nvarchar(40) NOT NULL,
    [before_data] nvarchar(max) NULL,
    [after_data] nvarchar(max) NULL,
    [occurred_at] datetime2(0) NOT NULL,
    CONSTRAINT [PK_AUDIT_EVENT] PRIMARY KEY ([audit_event_id]),
    CONSTRAINT [CK_AUDIT_EVENT_1] CHECK ([before_data] IS NULL OR ISJSON([before_data])=1),
    CONSTRAINT [CK_AUDIT_EVENT_2] CHECK ([after_data] IS NULL OR ISJSON([after_data])=1)
);

CREATE TABLE [dbo].[DATESHEET] (
    [datesheet_id] int IDENTITY(1,1) NOT NULL,
    [exam_cycle_id] int NOT NULL,
    [import_batch_id] int NULL,
    [revision_number] int NOT NULL,
    [status] nvarchar(20) NOT NULL CONSTRAINT [DF_DATESHEET_status] DEFAULT (N'DRAFT'),
    [published_by_user_id] int NULL,
    [published_at] datetime2(0) NULL,
    CONSTRAINT [PK_DATESHEET] PRIMARY KEY ([datesheet_id]),
    CONSTRAINT [UQ_DATESHEET_1] UNIQUE ([exam_cycle_id], [revision_number]),
    CONSTRAINT [UQ_DATESHEET_Cycle] UNIQUE ([datesheet_id], [exam_cycle_id]),
    CONSTRAINT [CK_DATESHEET_1] CHECK ([revision_number] > 0),
    CONSTRAINT [CK_DATESHEET_Status] CHECK ([status] IN (N'DRAFT', N'PUBLISHED', N'SUPERSEDED')),
    CONSTRAINT [CK_DATESHEET_Publication] CHECK (
        ([status] = N'DRAFT' AND [published_at] IS NULL AND [published_by_user_id] IS NULL)
        OR ([status] IN (N'PUBLISHED', N'SUPERSEDED') AND [published_at] IS NOT NULL AND [published_by_user_id] IS NOT NULL)
    )
);

CREATE TABLE [dbo].[EXAM_SCHEDULE] (
    [schedule_id] int IDENTITY(1,1) NOT NULL,
    [datesheet_id] int NOT NULL,
    [course_exam_id] int NOT NULL,
    [exam_cycle_id] int NOT NULL,
    [exam_date] date NOT NULL,
    [start_time] time(0) NOT NULL,
    [end_time] time(0) NOT NULL,
    CONSTRAINT [PK_EXAM_SCHEDULE] PRIMARY KEY ([schedule_id]),
    CONSTRAINT [UQ_EXAM_SCHEDULE_1] UNIQUE ([datesheet_id], [course_exam_id]),
    CONSTRAINT [CK_EXAM_SCHEDULE_1] CHECK ([end_time] > [start_time])
);

CREATE TABLE [dbo].[SYSTEM_SETTINGS] (
    [settings_id] int NOT NULL,
    [active_exam_cycle_id] int NULL,
    [active_datesheet_id] int NULL,
    [updated_by_user_id] int NULL,
    [updated_at] datetime2(0) NULL,
    CONSTRAINT [PK_SYSTEM_SETTINGS] PRIMARY KEY ([settings_id]),
    CONSTRAINT [CK_SYSTEM_SETTINGS_SingleRow] CHECK ([settings_id] = 1),
    CONSTRAINT [CK_SYSTEM_SETTINGS_ActivePair] CHECK (
        ([active_exam_cycle_id] IS NULL AND [active_datesheet_id] IS NULL)
        OR ([active_exam_cycle_id] IS NOT NULL AND [active_datesheet_id] IS NOT NULL)
    ),
    CONSTRAINT [CK_SYSTEM_SETTINGS_UpdatedPair] CHECK (
        ([updated_by_user_id] IS NULL AND [updated_at] IS NULL)
        OR ([updated_by_user_id] IS NOT NULL AND [updated_at] IS NOT NULL)
    ),
    CONSTRAINT [CK_SYSTEM_SETTINGS_ActiveAudit] CHECK (
        [active_exam_cycle_id] IS NULL
        OR ([updated_by_user_id] IS NOT NULL AND [updated_at] IS NOT NULL)
    )
);

-- Add FKs after all tables exist (Question <-> PaperItem is a cycle).
ALTER TABLE [dbo].[APP_USER] WITH CHECK ADD CONSTRAINT [FK_APP_USER_home_department_id] FOREIGN KEY ([home_department_id]) REFERENCES [dbo].[DEPARTMENT] ([department_id]);
ALTER TABLE [dbo].[USER_ROLE] WITH CHECK ADD CONSTRAINT [FK_USER_ROLE_user_id] FOREIGN KEY ([user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[USER_ROLE] WITH CHECK ADD CONSTRAINT [FK_USER_ROLE_role_id] FOREIGN KEY ([role_id]) REFERENCES [dbo].[ROLE] ([role_id]);
ALTER TABLE [dbo].[USER_ROLE] WITH CHECK ADD CONSTRAINT [FK_USER_ROLE_department_id] FOREIGN KEY ([department_id]) REFERENCES [dbo].[DEPARTMENT] ([department_id]);
ALTER TABLE [dbo].[DIRECTOR_DELEGATION] WITH CHECK ADD CONSTRAINT [FK_DIRECTOR_DELEGATION_grantor_user_role_id] FOREIGN KEY ([grantor_user_role_id]) REFERENCES [dbo].[USER_ROLE] ([user_role_id]);
ALTER TABLE [dbo].[DIRECTOR_DELEGATION] WITH CHECK ADD CONSTRAINT [FK_DIRECTOR_DELEGATION_delegate_user_id] FOREIGN KEY ([delegate_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[DIRECTOR_DELEGATION] WITH CHECK ADD CONSTRAINT [FK_DIRECTOR_DELEGATION_revoked_by_user_id] FOREIGN KEY ([revoked_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[PROGRAM] WITH CHECK ADD CONSTRAINT [FK_PROGRAM_department_id] FOREIGN KEY ([department_id]) REFERENCES [dbo].[DEPARTMENT] ([department_id]);
ALTER TABLE [dbo].[SECTION] WITH CHECK ADD CONSTRAINT [FK_SECTION_program_id] FOREIGN KEY ([program_id]) REFERENCES [dbo].[PROGRAM] ([program_id]);
ALTER TABLE [dbo].[SECTION] WITH CHECK ADD CONSTRAINT [FK_SECTION_session_id] FOREIGN KEY ([session_id]) REFERENCES [dbo].[ACADEMIC_SESSION] ([session_id]);
ALTER TABLE [dbo].[COURSE] WITH CHECK ADD CONSTRAINT [FK_COURSE_owner_department_id] FOREIGN KEY ([owner_department_id]) REFERENCES [dbo].[DEPARTMENT] ([department_id]);
ALTER TABLE [dbo].[COURSE_OUTLINE] WITH CHECK ADD CONSTRAINT [FK_COURSE_OUTLINE_course_id] FOREIGN KEY ([course_id]) REFERENCES [dbo].[COURSE] ([course_id]);
ALTER TABLE [dbo].[COURSE_OFFERING] WITH CHECK ADD CONSTRAINT [FK_COURSE_OFFERING_course_id] FOREIGN KEY ([course_id]) REFERENCES [dbo].[COURSE] ([course_id]);
ALTER TABLE [dbo].[COURSE_OFFERING] WITH CHECK ADD CONSTRAINT [FK_COURSE_OFFERING_outline_id] FOREIGN KEY ([outline_id]) REFERENCES [dbo].[COURSE_OUTLINE] ([outline_id]);
ALTER TABLE [dbo].[COURSE_OFFERING] WITH CHECK ADD CONSTRAINT [FK_COURSE_OFFERING_session_id] FOREIGN KEY ([session_id]) REFERENCES [dbo].[ACADEMIC_SESSION] ([session_id]);
ALTER TABLE [dbo].[COURSE_OFFERING] WITH CHECK ADD CONSTRAINT [FK_COURSE_OFFERING_lead_department_id] FOREIGN KEY ([lead_department_id]) REFERENCES [dbo].[DEPARTMENT] ([department_id]);
ALTER TABLE [dbo].[OFFERING_SECTION] WITH CHECK ADD CONSTRAINT [FK_OFFERING_SECTION_offering_id] FOREIGN KEY ([offering_id]) REFERENCES [dbo].[COURSE_OFFERING] ([offering_id]);
ALTER TABLE [dbo].[OFFERING_SECTION] WITH CHECK ADD CONSTRAINT [FK_OFFERING_SECTION_section_id] FOREIGN KEY ([section_id]) REFERENCES [dbo].[SECTION] ([section_id]);
ALTER TABLE [dbo].[TEACHING_ASSIGNMENT] WITH CHECK ADD CONSTRAINT [FK_TEACHING_ASSIGNMENT_offering_section_id] FOREIGN KEY ([offering_section_id]) REFERENCES [dbo].[OFFERING_SECTION] ([offering_section_id]);
ALTER TABLE [dbo].[TEACHING_ASSIGNMENT] WITH CHECK ADD CONSTRAINT [FK_TEACHING_ASSIGNMENT_teacher_user_id] FOREIGN KEY ([teacher_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[TOPIC] WITH CHECK ADD CONSTRAINT [FK_TOPIC_outline_id] FOREIGN KEY ([outline_id]) REFERENCES [dbo].[COURSE_OUTLINE] ([outline_id]);
ALTER TABLE [dbo].[CLO] WITH CHECK ADD CONSTRAINT [FK_CLO_outline_id] FOREIGN KEY ([outline_id]) REFERENCES [dbo].[COURSE_OUTLINE] ([outline_id]);
ALTER TABLE [dbo].[TOPIC_CLO] WITH CHECK ADD CONSTRAINT [FK_TOPIC_CLO_topic_id] FOREIGN KEY ([topic_id]) REFERENCES [dbo].[TOPIC] ([topic_id]);
ALTER TABLE [dbo].[TOPIC_CLO] WITH CHECK ADD CONSTRAINT [FK_TOPIC_CLO_clo_id] FOREIGN KEY ([clo_id]) REFERENCES [dbo].[CLO] ([clo_id]);
ALTER TABLE [dbo].[EXAM_CYCLE] WITH CHECK ADD CONSTRAINT [FK_EXAM_CYCLE_session_id] FOREIGN KEY ([session_id]) REFERENCES [dbo].[ACADEMIC_SESSION] ([session_id]);
ALTER TABLE [dbo].[COURSE_EXAM] WITH CHECK ADD CONSTRAINT [FK_COURSE_EXAM_offering_id] FOREIGN KEY ([offering_id]) REFERENCES [dbo].[COURSE_OFFERING] ([offering_id]);
ALTER TABLE [dbo].[COURSE_EXAM] WITH CHECK ADD CONSTRAINT [FK_COURSE_EXAM_exam_cycle_id] FOREIGN KEY ([exam_cycle_id]) REFERENCES [dbo].[EXAM_CYCLE] ([exam_cycle_id]);
ALTER TABLE [dbo].[COURSE_EXAM] WITH CHECK ADD CONSTRAINT [FK_COURSE_EXAM_configured_by_user_id] FOREIGN KEY ([configured_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[EXAM_CLO] WITH CHECK ADD CONSTRAINT [FK_EXAM_CLO_course_exam_id] FOREIGN KEY ([course_exam_id]) REFERENCES [dbo].[COURSE_EXAM] ([course_exam_id]);
ALTER TABLE [dbo].[EXAM_CLO] WITH CHECK ADD CONSTRAINT [FK_EXAM_CLO_clo_id] FOREIGN KEY ([clo_id]) REFERENCES [dbo].[CLO] ([clo_id]);
ALTER TABLE [dbo].[TOPIC_COVERAGE] WITH CHECK ADD CONSTRAINT [FK_TOPIC_COVERAGE_teaching_assignment_id] FOREIGN KEY ([teaching_assignment_id]) REFERENCES [dbo].[TEACHING_ASSIGNMENT] ([teaching_assignment_id]);
ALTER TABLE [dbo].[TOPIC_COVERAGE] WITH CHECK ADD CONSTRAINT [FK_TOPIC_COVERAGE_course_exam_id] FOREIGN KEY ([course_exam_id]) REFERENCES [dbo].[COURSE_EXAM] ([course_exam_id]);
ALTER TABLE [dbo].[TOPIC_COVERAGE] WITH CHECK ADD CONSTRAINT [FK_TOPIC_COVERAGE_topic_id] FOREIGN KEY ([topic_id]) REFERENCES [dbo].[TOPIC] ([topic_id]);
ALTER TABLE [dbo].[PAPER] WITH CHECK ADD CONSTRAINT [FK_PAPER_course_exam_id] FOREIGN KEY ([course_exam_id]) REFERENCES [dbo].[COURSE_EXAM] ([course_exam_id]);
ALTER TABLE [dbo].[PAPER] WITH CHECK ADD CONSTRAINT [FK_PAPER_deleted_by_user_id] FOREIGN KEY ([deleted_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[PAPER_ASSIGNMENT] WITH CHECK ADD CONSTRAINT [FK_PAPER_ASSIGNMENT_paper_id] FOREIGN KEY ([paper_id]) REFERENCES [dbo].[PAPER] ([paper_id]);
ALTER TABLE [dbo].[PAPER_ASSIGNMENT] WITH CHECK ADD CONSTRAINT [FK_PAPER_ASSIGNMENT_setter_user_id] FOREIGN KEY ([setter_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[PAPER_ASSIGNMENT] WITH CHECK ADD CONSTRAINT [FK_PAPER_ASSIGNMENT_assigned_by_user_role_id] FOREIGN KEY ([assigned_by_user_role_id]) REFERENCES [dbo].[USER_ROLE] ([user_role_id]);
ALTER TABLE [dbo].[PAPER_PARTICIPANT] WITH CHECK ADD CONSTRAINT [FK_PAPER_PARTICIPANT_paper_id] FOREIGN KEY ([paper_id]) REFERENCES [dbo].[PAPER] ([paper_id]);
ALTER TABLE [dbo].[PAPER_PARTICIPANT] WITH CHECK ADD CONSTRAINT [FK_PAPER_PARTICIPANT_teacher_user_id] FOREIGN KEY ([teacher_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[PAPER_VERSION] WITH CHECK ADD CONSTRAINT [FK_PAPER_VERSION_paper_id] FOREIGN KEY ([paper_id]) REFERENCES [dbo].[PAPER] ([paper_id]);
ALTER TABLE [dbo].[PAPER_VERSION] WITH CHECK ADD CONSTRAINT [FK_PAPER_VERSION_based_on_version_id] FOREIGN KEY ([based_on_version_id]) REFERENCES [dbo].[PAPER_VERSION] ([paper_version_id]);
ALTER TABLE [dbo].[PAPER_VERSION] WITH CHECK ADD CONSTRAINT [FK_PAPER_VERSION_copied_from_version_id] FOREIGN KEY ([copied_from_version_id]) REFERENCES [dbo].[PAPER_VERSION] ([paper_version_id]);
ALTER TABLE [dbo].[PAPER_VERSION] WITH CHECK ADD CONSTRAINT [FK_PAPER_VERSION_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[PAPER_SUBMISSION] WITH CHECK ADD CONSTRAINT [FK_PAPER_SUBMISSION_paper_version_id] FOREIGN KEY ([paper_version_id]) REFERENCES [dbo].[PAPER_VERSION] ([paper_version_id]);
ALTER TABLE [dbo].[PAPER_SUBMISSION] WITH CHECK ADD CONSTRAINT [FK_PAPER_SUBMISSION_submitted_by_user_id] FOREIGN KEY ([submitted_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[QUESTION] WITH CHECK ADD CONSTRAINT [FK_QUESTION_paper_id] FOREIGN KEY ([paper_id]) REFERENCES [dbo].[PAPER] ([paper_id]);
ALTER TABLE [dbo].[QUESTION] WITH CHECK ADD CONSTRAINT [FK_QUESTION_author_user_id] FOREIGN KEY ([author_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[QUESTION] WITH CHECK ADD CONSTRAINT [FK_QUESTION_source_paper_item_id] FOREIGN KEY ([source_paper_item_id]) REFERENCES [dbo].[PAPER_ITEM] ([paper_item_id]);
ALTER TABLE [dbo].[QUESTION] WITH CHECK ADD CONSTRAINT [FK_QUESTION_deleted_by_user_id] FOREIGN KEY ([deleted_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[QUESTION_VERSION] WITH CHECK ADD CONSTRAINT [FK_QUESTION_VERSION_question_id] FOREIGN KEY ([question_id]) REFERENCES [dbo].[QUESTION] ([question_id]);
ALTER TABLE [dbo].[QUESTION_VERSION] WITH CHECK ADD CONSTRAINT [FK_QUESTION_VERSION_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[QUESTION_PART] WITH CHECK ADD CONSTRAINT [FK_QUESTION_PART_question_version_id] FOREIGN KEY ([question_version_id]) REFERENCES [dbo].[QUESTION_VERSION] ([question_version_id]);
ALTER TABLE [dbo].[QUESTION_PART] WITH CHECK ADD CONSTRAINT [FK_QUESTION_PART_topic_id] FOREIGN KEY ([topic_id]) REFERENCES [dbo].[TOPIC] ([topic_id]);
ALTER TABLE [dbo].[PAPER_ITEM] WITH CHECK ADD CONSTRAINT [FK_PAPER_ITEM_paper_version_id] FOREIGN KEY ([paper_version_id]) REFERENCES [dbo].[PAPER_VERSION] ([paper_version_id]);
ALTER TABLE [dbo].[PAPER_ITEM] WITH CHECK ADD CONSTRAINT [FK_PAPER_ITEM_question_version_id] FOREIGN KEY ([question_version_id]) REFERENCES [dbo].[QUESTION_VERSION] ([question_version_id]);
ALTER TABLE [dbo].[REVIEW] WITH CHECK ADD CONSTRAINT [FK_REVIEW_submission_id] FOREIGN KEY ([submission_id]) REFERENCES [dbo].[PAPER_SUBMISSION] ([submission_id]);
ALTER TABLE [dbo].[REVIEW] WITH CHECK ADD CONSTRAINT [FK_REVIEW_reviewed_version_id] FOREIGN KEY ([reviewed_version_id]) REFERENCES [dbo].[PAPER_VERSION] ([paper_version_id]);
ALTER TABLE [dbo].[REVIEW] WITH CHECK ADD CONSTRAINT [FK_REVIEW_reviewer_user_id] FOREIGN KEY ([reviewer_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[REVIEW] WITH CHECK ADD CONSTRAINT [FK_REVIEW_reviewer_user_role_id] FOREIGN KEY ([reviewer_user_role_id]) REFERENCES [dbo].[USER_ROLE] ([user_role_id]);
ALTER TABLE [dbo].[REVIEW] WITH CHECK ADD CONSTRAINT [FK_REVIEW_delegation_id] FOREIGN KEY ([delegation_id]) REFERENCES [dbo].[DIRECTOR_DELEGATION] ([delegation_id]);
ALTER TABLE [dbo].[COMMENT] WITH CHECK ADD CONSTRAINT [FK_COMMENT_paper_version_id] FOREIGN KEY ([paper_version_id]) REFERENCES [dbo].[PAPER_VERSION] ([paper_version_id]);
ALTER TABLE [dbo].[COMMENT] WITH CHECK ADD CONSTRAINT [FK_COMMENT_paper_item_id] FOREIGN KEY ([paper_item_id]) REFERENCES [dbo].[PAPER_ITEM] ([paper_item_id]);
ALTER TABLE [dbo].[COMMENT] WITH CHECK ADD CONSTRAINT [FK_COMMENT_parent_comment_id] FOREIGN KEY ([parent_comment_id]) REFERENCES [dbo].[COMMENT] ([comment_id]);
ALTER TABLE [dbo].[COMMENT] WITH CHECK ADD CONSTRAINT [FK_COMMENT_author_user_id] FOREIGN KEY ([author_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[COMMENT] WITH CHECK ADD CONSTRAINT [FK_COMMENT_acknowledged_by_user_id] FOREIGN KEY ([acknowledged_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[PRINT_EXPORT] WITH CHECK ADD CONSTRAINT [FK_PRINT_EXPORT_approval_review_id] FOREIGN KEY ([approval_review_id]) REFERENCES [dbo].[REVIEW] ([review_id]);
ALTER TABLE [dbo].[PRINT_EXPORT] WITH CHECK ADD CONSTRAINT [FK_PRINT_EXPORT_requested_by_user_id] FOREIGN KEY ([requested_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[PRINT_EXPORT] WITH CHECK ADD CONSTRAINT [FK_PRINT_EXPORT_file_asset_id] FOREIGN KEY ([file_asset_id]) REFERENCES [dbo].[FILE_ASSET] ([file_asset_id]);
ALTER TABLE [dbo].[PHYSICAL_RECEIPT] WITH CHECK ADD CONSTRAINT [FK_PHYSICAL_RECEIPT_approval_review_id] FOREIGN KEY ([approval_review_id]) REFERENCES [dbo].[REVIEW] ([review_id]);
ALTER TABLE [dbo].[PHYSICAL_RECEIPT] WITH CHECK ADD CONSTRAINT [FK_PHYSICAL_RECEIPT_received_by_user_id] FOREIGN KEY ([received_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[PHYSICAL_RECEIPT] WITH CHECK ADD CONSTRAINT [FK_PHYSICAL_RECEIPT_delivered_by_user_id] FOREIGN KEY ([delivered_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[SIMILARITY_RUN] WITH CHECK ADD CONSTRAINT [FK_SIMILARITY_RUN_paper_version_id] FOREIGN KEY ([paper_version_id]) REFERENCES [dbo].[PAPER_VERSION] ([paper_version_id]);
ALTER TABLE [dbo].[QUESTION_MATCH] WITH CHECK ADD CONSTRAINT [FK_QUESTION_MATCH_similarity_run_id] FOREIGN KEY ([similarity_run_id]) REFERENCES [dbo].[SIMILARITY_RUN] ([similarity_run_id]);
ALTER TABLE [dbo].[QUESTION_MATCH] WITH CHECK ADD CONSTRAINT [FK_QUESTION_MATCH_current_item_id] FOREIGN KEY ([current_item_id]) REFERENCES [dbo].[PAPER_ITEM] ([paper_item_id]);
ALTER TABLE [dbo].[QUESTION_MATCH] WITH CHECK ADD CONSTRAINT [FK_QUESTION_MATCH_source_item_id] FOREIGN KEY ([source_item_id]) REFERENCES [dbo].[PAPER_ITEM] ([paper_item_id]);
ALTER TABLE [dbo].[PAPER_MATCH] WITH CHECK ADD CONSTRAINT [FK_PAPER_MATCH_similarity_run_id] FOREIGN KEY ([similarity_run_id]) REFERENCES [dbo].[SIMILARITY_RUN] ([similarity_run_id]);
ALTER TABLE [dbo].[PAPER_MATCH] WITH CHECK ADD CONSTRAINT [FK_PAPER_MATCH_source_paper_version_id] FOREIGN KEY ([source_paper_version_id]) REFERENCES [dbo].[PAPER_VERSION] ([paper_version_id]);
ALTER TABLE [dbo].[FILE_ASSET] WITH CHECK ADD CONSTRAINT [FK_FILE_ASSET_uploaded_by_user_id] FOREIGN KEY ([uploaded_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[QUESTION_ATTACHMENT] WITH CHECK ADD CONSTRAINT [FK_QUESTION_ATTACHMENT_question_version_id] FOREIGN KEY ([question_version_id]) REFERENCES [dbo].[QUESTION_VERSION] ([question_version_id]);
ALTER TABLE [dbo].[QUESTION_ATTACHMENT] WITH CHECK ADD CONSTRAINT [FK_QUESTION_ATTACHMENT_question_part_id] FOREIGN KEY ([question_part_id]) REFERENCES [dbo].[QUESTION_PART] ([question_part_id]);
ALTER TABLE [dbo].[QUESTION_ATTACHMENT] WITH CHECK ADD CONSTRAINT [FK_QUESTION_ATTACHMENT_file_asset_id] FOREIGN KEY ([file_asset_id]) REFERENCES [dbo].[FILE_ASSET] ([file_asset_id]);
ALTER TABLE [dbo].[IMPORT_BATCH] WITH CHECK ADD CONSTRAINT [FK_IMPORT_BATCH_file_asset_id] FOREIGN KEY ([file_asset_id]) REFERENCES [dbo].[FILE_ASSET] ([file_asset_id]);
ALTER TABLE [dbo].[IMPORT_BATCH] WITH CHECK ADD CONSTRAINT [FK_IMPORT_BATCH_imported_by_user_id] FOREIGN KEY ([imported_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[IMPORT_ERROR] WITH CHECK ADD CONSTRAINT [FK_IMPORT_ERROR_import_batch_id] FOREIGN KEY ([import_batch_id]) REFERENCES [dbo].[IMPORT_BATCH] ([import_batch_id]);
ALTER TABLE [dbo].[WISHLIST] WITH CHECK ADD CONSTRAINT [FK_WISHLIST_owner_user_id] FOREIGN KEY ([owner_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[WISHLIST] WITH CHECK ADD CONSTRAINT [FK_WISHLIST_course_id] FOREIGN KEY ([course_id]) REFERENCES [dbo].[COURSE] ([course_id]);
ALTER TABLE [dbo].[WISHLIST] WITH CHECK ADD CONSTRAINT [FK_WISHLIST_paper_id] FOREIGN KEY ([paper_id]) REFERENCES [dbo].[PAPER] ([paper_id]);
ALTER TABLE [dbo].[WISHLIST] WITH CHECK ADD CONSTRAINT [FK_WISHLIST_deleted_by_user_id] FOREIGN KEY ([deleted_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[NOTIFICATION] WITH CHECK ADD CONSTRAINT [FK_NOTIFICATION_recipient_user_id] FOREIGN KEY ([recipient_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[NOTIFICATION] WITH CHECK ADD CONSTRAINT [FK_NOTIFICATION_paper_id] FOREIGN KEY ([paper_id]) REFERENCES [dbo].[PAPER] ([paper_id]);
ALTER TABLE [dbo].[NOTIFICATION] WITH CHECK ADD CONSTRAINT [FK_NOTIFICATION_delegation_id] FOREIGN KEY ([delegation_id]) REFERENCES [dbo].[DIRECTOR_DELEGATION] ([delegation_id]);
ALTER TABLE [dbo].[AUDIT_EVENT] WITH CHECK ADD CONSTRAINT [FK_AUDIT_EVENT_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[AUDIT_EVENT] WITH CHECK ADD CONSTRAINT [FK_AUDIT_EVENT_delegation_id] FOREIGN KEY ([delegation_id]) REFERENCES [dbo].[DIRECTOR_DELEGATION] ([delegation_id]);
ALTER TABLE [dbo].[DATESHEET] WITH CHECK ADD CONSTRAINT [FK_DATESHEET_exam_cycle_id] FOREIGN KEY ([exam_cycle_id]) REFERENCES [dbo].[EXAM_CYCLE] ([exam_cycle_id]);
ALTER TABLE [dbo].[DATESHEET] WITH CHECK ADD CONSTRAINT [FK_DATESHEET_import_batch_id] FOREIGN KEY ([import_batch_id]) REFERENCES [dbo].[IMPORT_BATCH] ([import_batch_id]);
ALTER TABLE [dbo].[EXAM_SCHEDULE] WITH CHECK ADD CONSTRAINT [FK_EXAM_SCHEDULE_datesheet_id] FOREIGN KEY ([datesheet_id]) REFERENCES [dbo].[DATESHEET] ([datesheet_id]);
ALTER TABLE [dbo].[EXAM_SCHEDULE] WITH CHECK ADD CONSTRAINT [FK_EXAM_SCHEDULE_course_exam_id] FOREIGN KEY ([course_exam_id]) REFERENCES [dbo].[COURSE_EXAM] ([course_exam_id]);

CREATE UNIQUE INDEX [UX_PAPER_ASSIGNMENT_Current] ON [dbo].[PAPER_ASSIGNMENT] ([paper_id]) WHERE [ended_at] IS NULL;
CREATE UNIQUE INDEX [UX_TEACHING_ASSIGNMENT_Current] ON [dbo].[TEACHING_ASSIGNMENT] ([offering_section_id],[teacher_user_id]) WHERE [ends_at] IS NULL;
CREATE UNIQUE INDEX [UX_USER_ROLE_Open] ON [dbo].[USER_ROLE] ([user_id],[role_id],[department_id]) WHERE [valid_until] IS NULL;
CREATE INDEX [IX_APP_USER_home_department_id] ON [dbo].[APP_USER] ([home_department_id]);
CREATE INDEX [IX_USER_ROLE_user_id] ON [dbo].[USER_ROLE] ([user_id]);
CREATE INDEX [IX_USER_ROLE_role_id] ON [dbo].[USER_ROLE] ([role_id]);
CREATE INDEX [IX_USER_ROLE_department_id] ON [dbo].[USER_ROLE] ([department_id]);
CREATE INDEX [IX_DIRECTOR_DELEGATION_grantor_user_role_id] ON [dbo].[DIRECTOR_DELEGATION] ([grantor_user_role_id]);
CREATE INDEX [IX_DIRECTOR_DELEGATION_delegate_user_id] ON [dbo].[DIRECTOR_DELEGATION] ([delegate_user_id]);
CREATE INDEX [IX_DIRECTOR_DELEGATION_revoked_by_user_id] ON [dbo].[DIRECTOR_DELEGATION] ([revoked_by_user_id]);
CREATE INDEX [IX_PROGRAM_department_id] ON [dbo].[PROGRAM] ([department_id]);
CREATE INDEX [IX_SECTION_program_id] ON [dbo].[SECTION] ([program_id]);
CREATE INDEX [IX_SECTION_session_id] ON [dbo].[SECTION] ([session_id]);
CREATE INDEX [IX_COURSE_owner_department_id] ON [dbo].[COURSE] ([owner_department_id]);
CREATE INDEX [IX_COURSE_OUTLINE_course_id] ON [dbo].[COURSE_OUTLINE] ([course_id]);
CREATE INDEX [IX_COURSE_OFFERING_course_id] ON [dbo].[COURSE_OFFERING] ([course_id]);
CREATE INDEX [IX_COURSE_OFFERING_outline_id] ON [dbo].[COURSE_OFFERING] ([outline_id]);
CREATE INDEX [IX_COURSE_OFFERING_session_id] ON [dbo].[COURSE_OFFERING] ([session_id]);
CREATE INDEX [IX_COURSE_OFFERING_lead_department_id] ON [dbo].[COURSE_OFFERING] ([lead_department_id]);
CREATE INDEX [IX_OFFERING_SECTION_offering_id] ON [dbo].[OFFERING_SECTION] ([offering_id]);
CREATE INDEX [IX_OFFERING_SECTION_section_id] ON [dbo].[OFFERING_SECTION] ([section_id]);
CREATE INDEX [IX_TEACHING_ASSIGNMENT_offering_section_id] ON [dbo].[TEACHING_ASSIGNMENT] ([offering_section_id]);
CREATE INDEX [IX_TEACHING_ASSIGNMENT_teacher_user_id] ON [dbo].[TEACHING_ASSIGNMENT] ([teacher_user_id]);
CREATE INDEX [IX_TOPIC_outline_id] ON [dbo].[TOPIC] ([outline_id]);
CREATE INDEX [IX_CLO_outline_id] ON [dbo].[CLO] ([outline_id]);
CREATE INDEX [IX_EXAM_CYCLE_session_id] ON [dbo].[EXAM_CYCLE] ([session_id]);
CREATE INDEX [IX_COURSE_EXAM_offering_id] ON [dbo].[COURSE_EXAM] ([offering_id]);
CREATE INDEX [IX_COURSE_EXAM_exam_cycle_id] ON [dbo].[COURSE_EXAM] ([exam_cycle_id]);
CREATE INDEX [IX_COURSE_EXAM_configured_by_user_id] ON [dbo].[COURSE_EXAM] ([configured_by_user_id]);
CREATE INDEX [IX_EXAM_CLO_course_exam_id] ON [dbo].[EXAM_CLO] ([course_exam_id]);
CREATE INDEX [IX_EXAM_CLO_clo_id] ON [dbo].[EXAM_CLO] ([clo_id]);
CREATE INDEX [IX_TOPIC_COVERAGE_teaching_assignment_id] ON [dbo].[TOPIC_COVERAGE] ([teaching_assignment_id]);
CREATE INDEX [IX_TOPIC_COVERAGE_course_exam_id] ON [dbo].[TOPIC_COVERAGE] ([course_exam_id]);
CREATE INDEX [IX_TOPIC_COVERAGE_topic_id] ON [dbo].[TOPIC_COVERAGE] ([topic_id]);
CREATE INDEX [IX_PAPER_deleted_by_user_id] ON [dbo].[PAPER] ([deleted_by_user_id]);
CREATE INDEX [IX_PAPER_ASSIGNMENT_paper_id] ON [dbo].[PAPER_ASSIGNMENT] ([paper_id]);
CREATE INDEX [IX_PAPER_ASSIGNMENT_setter_user_id] ON [dbo].[PAPER_ASSIGNMENT] ([setter_user_id]);
CREATE INDEX [IX_PAPER_ASSIGNMENT_assigned_by_user_role_id] ON [dbo].[PAPER_ASSIGNMENT] ([assigned_by_user_role_id]);
CREATE INDEX [IX_PAPER_VERSION_paper_id] ON [dbo].[PAPER_VERSION] ([paper_id]);
CREATE INDEX [IX_PAPER_VERSION_based_on_version_id] ON [dbo].[PAPER_VERSION] ([based_on_version_id]);
CREATE INDEX [IX_PAPER_VERSION_copied_from_version_id] ON [dbo].[PAPER_VERSION] ([copied_from_version_id]);
CREATE INDEX [IX_PAPER_VERSION_created_by_user_id] ON [dbo].[PAPER_VERSION] ([created_by_user_id]);
CREATE INDEX [IX_PAPER_SUBMISSION_submitted_by_user_id] ON [dbo].[PAPER_SUBMISSION] ([submitted_by_user_id]);
CREATE INDEX [IX_QUESTION_paper_id] ON [dbo].[QUESTION] ([paper_id]);
CREATE INDEX [IX_QUESTION_author_user_id] ON [dbo].[QUESTION] ([author_user_id]);
CREATE INDEX [IX_QUESTION_source_paper_item_id] ON [dbo].[QUESTION] ([source_paper_item_id]);
CREATE INDEX [IX_QUESTION_deleted_by_user_id] ON [dbo].[QUESTION] ([deleted_by_user_id]);
CREATE INDEX [IX_QUESTION_VERSION_question_id] ON [dbo].[QUESTION_VERSION] ([question_id]);
CREATE INDEX [IX_QUESTION_VERSION_created_by_user_id] ON [dbo].[QUESTION_VERSION] ([created_by_user_id]);
CREATE INDEX [IX_QUESTION_PART_question_version_id] ON [dbo].[QUESTION_PART] ([question_version_id]);
CREATE INDEX [IX_QUESTION_PART_topic_id] ON [dbo].[QUESTION_PART] ([topic_id]);
CREATE INDEX [IX_PAPER_ITEM_paper_version_id] ON [dbo].[PAPER_ITEM] ([paper_version_id]);
CREATE INDEX [IX_PAPER_ITEM_question_version_id] ON [dbo].[PAPER_ITEM] ([question_version_id]);
CREATE INDEX [IX_REVIEW_submission_id] ON [dbo].[REVIEW] ([submission_id]);
CREATE INDEX [IX_REVIEW_reviewed_version_id] ON [dbo].[REVIEW] ([reviewed_version_id]);
CREATE INDEX [IX_REVIEW_reviewer_user_id] ON [dbo].[REVIEW] ([reviewer_user_id]);
CREATE INDEX [IX_REVIEW_reviewer_user_role_id] ON [dbo].[REVIEW] ([reviewer_user_role_id]);
CREATE INDEX [IX_REVIEW_delegation_id] ON [dbo].[REVIEW] ([delegation_id]);
CREATE INDEX [IX_COMMENT_paper_version_id] ON [dbo].[COMMENT] ([paper_version_id]);
CREATE INDEX [IX_COMMENT_paper_item_id] ON [dbo].[COMMENT] ([paper_item_id]);
CREATE INDEX [IX_COMMENT_parent_comment_id] ON [dbo].[COMMENT] ([parent_comment_id]);
CREATE INDEX [IX_COMMENT_author_user_id] ON [dbo].[COMMENT] ([author_user_id]);
CREATE INDEX [IX_COMMENT_acknowledged_by_user_id] ON [dbo].[COMMENT] ([acknowledged_by_user_id]);
CREATE INDEX [IX_PRINT_EXPORT_approval_review_id] ON [dbo].[PRINT_EXPORT] ([approval_review_id]);
CREATE INDEX [IX_PRINT_EXPORT_requested_by_user_id] ON [dbo].[PRINT_EXPORT] ([requested_by_user_id]);
CREATE INDEX [IX_PRINT_EXPORT_file_asset_id] ON [dbo].[PRINT_EXPORT] ([file_asset_id]);
CREATE INDEX [IX_PHYSICAL_RECEIPT_received_by_user_id] ON [dbo].[PHYSICAL_RECEIPT] ([received_by_user_id]);
CREATE INDEX [IX_PHYSICAL_RECEIPT_delivered_by_user_id] ON [dbo].[PHYSICAL_RECEIPT] ([delivered_by_user_id]);
CREATE INDEX [IX_SIMILARITY_RUN_paper_version_id] ON [dbo].[SIMILARITY_RUN] ([paper_version_id]);
CREATE INDEX [IX_QUESTION_MATCH_similarity_run_id] ON [dbo].[QUESTION_MATCH] ([similarity_run_id]);
CREATE INDEX [IX_QUESTION_MATCH_current_item_id] ON [dbo].[QUESTION_MATCH] ([current_item_id]);
CREATE INDEX [IX_QUESTION_MATCH_source_item_id] ON [dbo].[QUESTION_MATCH] ([source_item_id]);
CREATE INDEX [IX_PAPER_MATCH_similarity_run_id] ON [dbo].[PAPER_MATCH] ([similarity_run_id]);
CREATE INDEX [IX_PAPER_MATCH_source_paper_version_id] ON [dbo].[PAPER_MATCH] ([source_paper_version_id]);
CREATE INDEX [IX_FILE_ASSET_uploaded_by_user_id] ON [dbo].[FILE_ASSET] ([uploaded_by_user_id]);
CREATE INDEX [IX_QUESTION_ATTACHMENT_question_version_id] ON [dbo].[QUESTION_ATTACHMENT] ([question_version_id]);
CREATE INDEX [IX_QUESTION_ATTACHMENT_question_part_id] ON [dbo].[QUESTION_ATTACHMENT] ([question_part_id]);
CREATE INDEX [IX_QUESTION_ATTACHMENT_file_asset_id] ON [dbo].[QUESTION_ATTACHMENT] ([file_asset_id]);
CREATE INDEX [IX_IMPORT_BATCH_file_asset_id] ON [dbo].[IMPORT_BATCH] ([file_asset_id]);
CREATE INDEX [IX_IMPORT_BATCH_imported_by_user_id] ON [dbo].[IMPORT_BATCH] ([imported_by_user_id]);
CREATE INDEX [IX_IMPORT_ERROR_import_batch_id] ON [dbo].[IMPORT_ERROR] ([import_batch_id]);
CREATE INDEX [IX_WISHLIST_owner_user_id] ON [dbo].[WISHLIST] ([owner_user_id]);
CREATE INDEX [IX_WISHLIST_course_id] ON [dbo].[WISHLIST] ([course_id]);
CREATE INDEX [IX_WISHLIST_paper_id] ON [dbo].[WISHLIST] ([paper_id]);
CREATE INDEX [IX_WISHLIST_deleted_by_user_id] ON [dbo].[WISHLIST] ([deleted_by_user_id]);
CREATE INDEX [IX_NOTIFICATION_recipient_user_id] ON [dbo].[NOTIFICATION] ([recipient_user_id]);
CREATE INDEX [IX_NOTIFICATION_paper_id] ON [dbo].[NOTIFICATION] ([paper_id]);
CREATE INDEX [IX_NOTIFICATION_delegation_id] ON [dbo].[NOTIFICATION] ([delegation_id]);
CREATE INDEX [IX_AUDIT_EVENT_actor_user_id] ON [dbo].[AUDIT_EVENT] ([actor_user_id]);
CREATE INDEX [IX_AUDIT_EVENT_delegation_id] ON [dbo].[AUDIT_EVENT] ([delegation_id]);
CREATE INDEX [IX_DATESHEET_exam_cycle_id] ON [dbo].[DATESHEET] ([exam_cycle_id]);
CREATE INDEX [IX_DATESHEET_import_batch_id] ON [dbo].[DATESHEET] ([import_batch_id]);
CREATE INDEX [IX_EXAM_SCHEDULE_datesheet_id] ON [dbo].[EXAM_SCHEDULE] ([datesheet_id]);
CREATE INDEX [IX_EXAM_SCHEDULE_course_exam_id] ON [dbo].[EXAM_SCHEDULE] ([course_exam_id]);

CREATE TABLE dbo.EXAM_COMMON_TOPIC (
 common_topic_id int IDENTITY(1,1) PRIMARY KEY,
 course_exam_id int NOT NULL REFERENCES dbo.COURSE_EXAM(course_exam_id),
 topic_id int NOT NULL REFERENCES dbo.TOPIC(topic_id),
 computed_at datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
 UNIQUE(course_exam_id, topic_id)
);
CREATE TABLE dbo.CLO_MAPPING_CHECK (
 check_id int IDENTITY(1,1) PRIMARY KEY,
 mapping_id int NOT NULL REFERENCES dbo.CLO_MAPPING(mapping_id),
 check_status nvarchar(30) NOT NULL CHECK(check_status IN ('ALIGNED','NOT_ALIGNED','NEEDS_REVIEW')),
 explanation nvarchar(max) NOT NULL,
 checker_version nvarchar(100) NOT NULL,
 checked_at datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE dbo.PAPER_TOPIC_CHECK (
 topic_check_id int IDENTITY(1,1) PRIMARY KEY,
 paper_version_id int NOT NULL REFERENCES dbo.PAPER_VERSION(paper_version_id),
 question_part_id int NOT NULL REFERENCES dbo.QUESTION_PART(question_part_id),
 common_topic_id int NULL REFERENCES dbo.EXAM_COMMON_TOPIC(common_topic_id),
 check_status nvarchar(20) NOT NULL CHECK(check_status IN ('ELIGIBLE','INELIGIBLE')),
 explanation nvarchar(1000) NOT NULL,
 checked_at datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
 CHECK ((check_status='ELIGIBLE' AND common_topic_id IS NOT NULL) OR (check_status='INELIGIBLE' AND common_topic_id IS NULL))
);
ALTER TABLE dbo.QUESTION_PART ADD CONSTRAINT UQ_Part_Revision UNIQUE(question_part_id,question_version_id);
ALTER TABLE dbo.CLO_MAPPING ADD CONSTRAINT FK_Mapping_Revision FOREIGN KEY(question_version_id) REFERENCES dbo.QUESTION_VERSION(question_version_id);
ALTER TABLE dbo.CLO_MAPPING ADD CONSTRAINT FK_Mapping_Part_Revision FOREIGN KEY(question_part_id,question_version_id) REFERENCES dbo.QUESTION_PART(question_part_id,question_version_id);
ALTER TABLE dbo.CLO_MAPPING ADD CONSTRAINT FK_Mapping_ExamCLO FOREIGN KEY(exam_clo_id) REFERENCES dbo.EXAM_CLO(exam_clo_id);
CREATE INDEX IX_Mapping_Check_History ON dbo.CLO_MAPPING_CHECK(mapping_id,check_id);
GO

ALTER TABLE [dbo].[DATESHEET] WITH CHECK ADD CONSTRAINT [FK_DATESHEET_published_by_user_id]
    FOREIGN KEY ([published_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
ALTER TABLE [dbo].[EXAM_SCHEDULE] WITH CHECK ADD CONSTRAINT [FK_EXAM_SCHEDULE_DatesheetCycle]
    FOREIGN KEY ([datesheet_id], [exam_cycle_id]) REFERENCES [dbo].[DATESHEET] ([datesheet_id], [exam_cycle_id]);
ALTER TABLE [dbo].[EXAM_SCHEDULE] WITH CHECK ADD CONSTRAINT [FK_EXAM_SCHEDULE_CourseExamCycle]
    FOREIGN KEY ([course_exam_id], [exam_cycle_id]) REFERENCES [dbo].[COURSE_EXAM] ([course_exam_id], [exam_cycle_id]);
ALTER TABLE [dbo].[SYSTEM_SETTINGS] WITH CHECK ADD CONSTRAINT [FK_SYSTEM_SETTINGS_ExamCycle]
    FOREIGN KEY ([active_exam_cycle_id]) REFERENCES [dbo].[EXAM_CYCLE] ([exam_cycle_id]);
ALTER TABLE [dbo].[SYSTEM_SETTINGS] WITH CHECK ADD CONSTRAINT [FK_SYSTEM_SETTINGS_DatesheetCycle]
    FOREIGN KEY ([active_datesheet_id], [active_exam_cycle_id]) REFERENCES [dbo].[DATESHEET] ([datesheet_id], [exam_cycle_id]);
ALTER TABLE [dbo].[SYSTEM_SETTINGS] WITH CHECK ADD CONSTRAINT [FK_SYSTEM_SETTINGS_UpdatedBy]
    FOREIGN KEY ([updated_by_user_id]) REFERENCES [dbo].[APP_USER] ([user_id]);
CREATE UNIQUE INDEX [UX_DATESHEET_PublishedCycle]
    ON [dbo].[DATESHEET] ([exam_cycle_id]) WHERE [status] = N'PUBLISHED';
CREATE INDEX [IX_DATESHEET_published_by_user_id] ON [dbo].[DATESHEET] ([published_by_user_id]);
CREATE INDEX [IX_EXAM_SCHEDULE_exam_cycle_id] ON [dbo].[EXAM_SCHEDULE] ([exam_cycle_id]);
-- First confirmed datesheet will set the current exam. No exam is guessed here.
INSERT INTO [dbo].[SYSTEM_SETTINGS] ([settings_id]) VALUES (1);
GO


-- Copy the complete validated common-topic set into this table when freezing a version.
CREATE TABLE [dbo].[PAPER_VERSION_COMMON_TOPIC] (
    [paper_version_id] int NOT NULL,
    [topic_id] int NOT NULL,
    [topic_title_snapshot] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_PAPER_VERSION_COMMON_TOPIC] PRIMARY KEY ([paper_version_id], [topic_id]),
    CONSTRAINT [FK_PaperVersionTopic_Version] FOREIGN KEY ([paper_version_id]) REFERENCES dbo.PAPER_VERSION(paper_version_id),
    CONSTRAINT [FK_PaperVersionTopic_Topic] FOREIGN KEY ([topic_id]) REFERENCES dbo.TOPIC(topic_id)
);
GO

ALTER TABLE dbo.APP_USER ADD CONSTRAINT FK_AppUser_ProfilePhoto FOREIGN KEY (profile_photo_file_asset_id) REFERENCES dbo.FILE_ASSET(file_asset_id);
ALTER TABLE dbo.TOPIC ADD CONSTRAINT FK_Topic_ParentOutline FOREIGN KEY (parent_topic_id,outline_id) REFERENCES dbo.TOPIC(topic_id,outline_id);
ALTER TABLE dbo.QUESTION_PART ADD CONSTRAINT FK_QuestionPart_ParentVersion FOREIGN KEY (parent_part_id,question_version_id) REFERENCES dbo.QUESTION_PART(question_part_id,question_version_id);
ALTER TABLE dbo.TOPIC_COVERAGE_SUBMISSION ADD CONSTRAINT FK_CoverageSubmission_Assignment FOREIGN KEY (teaching_assignment_id) REFERENCES dbo.TEACHING_ASSIGNMENT(teaching_assignment_id);
ALTER TABLE dbo.TOPIC_COVERAGE_SUBMISSION ADD CONSTRAINT FK_CoverageSubmission_Exam FOREIGN KEY (course_exam_id) REFERENCES dbo.COURSE_EXAM(course_exam_id);
ALTER TABLE dbo.TOPIC_COVERAGE_SUBMISSION ADD CONSTRAINT FK_CoverageSubmission_Submitter FOREIGN KEY (submitted_by_user_id) REFERENCES dbo.APP_USER(user_id);
ALTER TABLE dbo.TOPIC_COVERAGE ADD CONSTRAINT FK_TopicCoverage_Submission FOREIGN KEY (teaching_assignment_id,course_exam_id) REFERENCES dbo.TOPIC_COVERAGE_SUBMISSION(teaching_assignment_id,course_exam_id);
ALTER TABLE dbo.PAPER_SUBMISSION ADD CONSTRAINT UQ_Submission_Version UNIQUE(submission_id,paper_version_id);
ALTER TABLE dbo.REVIEW ADD CONSTRAINT FK_Review_SubmissionVersion FOREIGN KEY(submission_id,reviewed_version_id) REFERENCES dbo.PAPER_SUBMISSION(submission_id,paper_version_id);
ALTER TABLE dbo.QUESTION_ATTACHMENT ADD CONSTRAINT FK_Attachment_PartVersion FOREIGN KEY(question_part_id,question_version_id) REFERENCES dbo.QUESTION_PART(question_part_id,question_version_id);
ALTER TABLE dbo.COURSE_OUTLINE ADD CONSTRAINT UQ_Outline_Course UNIQUE(outline_id,course_id);
ALTER TABLE dbo.COURSE_OFFERING ADD CONSTRAINT FK_Offering_OutlineCourse FOREIGN KEY(outline_id,course_id) REFERENCES dbo.COURSE_OUTLINE(outline_id,course_id);
ALTER TABLE dbo.PAPER_VERSION ADD CONSTRAINT UQ_PaperVersion_Paper UNIQUE(paper_version_id,paper_id);
ALTER TABLE dbo.PAPER_VERSION ADD CONSTRAINT FK_PaperVersion_BasedOnPaper FOREIGN KEY(based_on_version_id,paper_id) REFERENCES dbo.PAPER_VERSION(paper_version_id,paper_id);
ALTER TABLE dbo.PAPER_VERSION ADD CONSTRAINT CK_PaperVersion_NotSelf CHECK(based_on_version_id IS NULL OR based_on_version_id <> paper_version_id);
ALTER TABLE dbo.PAPER_ITEM ADD CONSTRAINT UQ_PaperItem_Version UNIQUE(paper_item_id,paper_version_id);
ALTER TABLE dbo.COMMENT ADD CONSTRAINT FK_Comment_ItemVersion FOREIGN KEY(paper_item_id,paper_version_id) REFERENCES dbo.PAPER_ITEM(paper_item_id,paper_version_id);
CREATE INDEX IX_Topic_Parent ON dbo.TOPIC(parent_topic_id,outline_id);
CREATE INDEX IX_Part_Parent ON dbo.QUESTION_PART(parent_part_id,question_version_id);
CREATE INDEX IX_CoverageSubmission_Exam ON dbo.TOPIC_COVERAGE_SUBMISSION(course_exam_id,submission_status);
CREATE INDEX IX_AppUser_Photo ON dbo.APP_USER(profile_photo_file_asset_id);
GO

-- Essential lookup data, not dummy accounts.
INSERT INTO dbo.ROLE (role_code) VALUES (N'TEACHER'),(N'HOD'),(N'DIRECTOR'),(N'DATA_CELL');
GO
/* REQUIRED PROCEDURE / API CONTRACT (procedures will be delivered separately)
These rules are NOT automatically enforced by table definitions alone.

1. Roles and workflow
   Only HOD assigns the setter. Only the current setter submits.
   SUBMITTED -> HOD APPROVED review -> HOD_REVIEWED -> Director APPROVED -> APPROVED.
   Either review stage may return CHANGES_REQUESTED with mandatory feedback.
   HOD approval is a review gate, never final print approval.
   A correction creates a new paper version and submission; repeat both review stages.
   Only an active, authorized Director/delegate may approve, export or authorize receipt.
   Data Cell records receipt of the exact Director-approved version.
   Enforce department scope, role validity and delegation dates on every operation.

2. Topics and coverage
   TOPIC supports parent/child topics in the same outline; reject ancestor cycles.
   Create coverage headers for every required teaching assignment in the exam.
   Missing/DRAFT headers are pending; SUBMITTED may legitimately have zero topics.
   Coverage details are scoped to that teacher's assigned section, course and exam.
   Wait for ALL required submissions; calculate the intersection across ALL relevant
   sections/assignments, not just respondents. An empty intersection blocks paper setting.
   Reject topics/CLOs outside the course outline and session/exam mismatches.
   Reopening coverage invalidates current common-topic checks; keep frozen snapshots.

3. Questions, parts and marks
   Simple question: one root ITEM, label MAIN, order 1; UI hides the part wrapper.
   QUESTION_VERSION.prompt_text stores the question stem; do not print it twice.
   Multi-part question: root ITEMs a,b; a GROUP can contain ITEM subparts i,ii.
   Only GROUP nodes may have children; GROUP nodes must have at least one child.
   Reject ancestor cycles. Parent and child must belong to the same question version.
   ITEM stores marks, topic and difficulty. GROUP has no own marks/topic/difficulty.
   Sum ITEM marks once; never add group totals again. EXTRA items do not affect totals.
   Each MAIN question must have at least one ITEM; total must equal version required marks
   before submission. Validate all ITEM topics against the locked common-topic set.
   Multiple CLO mappings per question/part are allowed. CLOs carry NO mark allocation.
   Meaning-based CLO checks run in the checking service; store results with explanations.

4. Versions and concurrency
   Copy course-exam marks/duration into PAPER_VERSION snapshot columns at creation.
   At submission, atomically validate the paper, populate PAPER_VERSION_COMMON_TOPIC,
   calculate content_hash, set frozen_at, and insert PAPER_SUBMISSION.
   Never update/delete frozen content, its questions/parts/mappings/attachments or scope.
   Clone affected records for edits, including Director edits, and re-run checks/reviews.
   Scope topics and CLO definitions used by frozen papers must not be overwritten;
   publish a new outline version instead. Comments/reviews remain separate append records.
   Use row_version for optimistic concurrency; transactions/locks protect final transitions.
   Limit application writes to authorized procedures; a timestamp alone is not a lock.

5. Similarity
   Run EXACT and SEMANTIC checks separately, tied to a specific immutable paper version.
   Compare with eligible prior approved versions of the SAME course, excluding this paper
   and EXTRA questions. Record chosen exam/session policy and corpus in corpus_scope.
   EXACT per-source percentage = 100 * distinct matching current MAIN items / total
   current MAIN items. A MAIN question includes its stem and all parts/subparts.
   Partial-part matches do not count as a complete exact-question match.
   Define text normalization in algorithm_version; do not equate semantic score with reuse.
   Denominator zero means no valid completed report. Duplicate source matches count once.
   Store separate PAPER_MATCH rows for each source paper. Never sum source percentages.
   overall_percent, when used for EXACT, means the percentage of distinct current MAIN
   questions found in ANY source, counted once. It is not the sum of PAPER_MATCH values.
   Validate that current_item belongs to the run version and source_item to its source.

6. Datesheet and current exam
   Upload -> validate preview/session/exam/course/date/time -> Confirm & Set Current.
   Upload alone never changes global defaults. In ONE transaction lock SYSTEM_SETTINGS,
   supersede the previous published revision of that cycle, publish the validated sheet,
   update both active IDs and audit fields, then commit. Only PUBLISHED sheets may be current.
   All roles read the same global default; personal historical filters do not change it.
   No active exam means show Current exam not set. Do not infer it from today's date.
   Use active_datesheet_id for schedules so revised datesheets are handled correctly.

7. Fresh schema compatibility
   Do not execute older dummy-data scripts unchanged: coverage requires a header first;
   paper versions require marks/duration snapshots; ITEM parts require difficulty;
   HOD decisions are APPROVED/CHANGES_REQUESTED; ROLE rows are already seeded here.
*/

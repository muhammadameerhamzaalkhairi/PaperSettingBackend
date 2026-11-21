using System;
using System.Collections.Generic;
using BIITPaperSettingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BIITPaperSettingAPI.Data;

public partial class BiitDbContext : DbContext
{
    public BiitDbContext(DbContextOptions<BiitDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicSession> AcademicSessions { get; set; }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<AuditEvent> AuditEvents { get; set; }

    public virtual DbSet<Clo> Clos { get; set; }

    public virtual DbSet<CloMapping> CloMappings { get; set; }

    public virtual DbSet<CloMappingCheck> CloMappingChecks { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseExam> CourseExams { get; set; }

    public virtual DbSet<CourseOffering> CourseOfferings { get; set; }

    public virtual DbSet<CourseOutline> CourseOutlines { get; set; }

    public virtual DbSet<Datesheet> Datesheets { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<DirectorDelegation> DirectorDelegations { get; set; }

    public virtual DbSet<ErrorLog> ErrorLogs { get; set; }

    public virtual DbSet<ExamClo> ExamClos { get; set; }

    public virtual DbSet<ExamCommonTopic> ExamCommonTopics { get; set; }

    public virtual DbSet<ExamCycle> ExamCycles { get; set; }

    public virtual DbSet<ExamSchedule> ExamSchedules { get; set; }

    public virtual DbSet<FileAsset> FileAssets { get; set; }

    public virtual DbSet<ImportBatch> ImportBatches { get; set; }

    public virtual DbSet<ImportError> ImportErrors { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OfferingSection> OfferingSections { get; set; }

    public virtual DbSet<Paper> Papers { get; set; }

    public virtual DbSet<PaperAssignment> PaperAssignments { get; set; }

    public virtual DbSet<PaperItem> PaperItems { get; set; }

    public virtual DbSet<PaperMatch> PaperMatches { get; set; }

    public virtual DbSet<PaperParticipant> PaperParticipants { get; set; }

    public virtual DbSet<PaperSubmission> PaperSubmissions { get; set; }

    public virtual DbSet<PaperTopicCheck> PaperTopicChecks { get; set; }

    public virtual DbSet<PaperVersion> PaperVersions { get; set; }

    public virtual DbSet<PaperVersionCommonTopic> PaperVersionCommonTopics { get; set; }

    public virtual DbSet<PhysicalReceipt> PhysicalReceipts { get; set; }

    public virtual DbSet<PrintExport> PrintExports { get; set; }

    public virtual DbSet<BIITPaperSettingAPI.Models.Program> Programs { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAttachment> QuestionAttachments { get; set; }

    public virtual DbSet<QuestionMatch> QuestionMatches { get; set; }

    public virtual DbSet<QuestionPart> QuestionParts { get; set; }

    public virtual DbSet<QuestionVersion> QuestionVersions { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Section> Sections { get; set; }

    public virtual DbSet<SimilarityRun> SimilarityRuns { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<TeachingAssignment> TeachingAssignments { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<TopicCoverage> TopicCoverages { get; set; }

    public virtual DbSet<TopicCoverageSubmission> TopicCoverageSubmissions { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<VwActiveUserRole> VwActiveUserRoles { get; set; }

    public virtual DbSet<VwComputedCommonTopic> VwComputedCommonTopics { get; set; }

    public virtual DbSet<VwCourseExam> VwCourseExams { get; set; }

    public virtual DbSet<VwCurrentExam> VwCurrentExams { get; set; }

    public virtual DbSet<VwDatesheetHistory> VwDatesheetHistories { get; set; }

    public virtual DbSet<VwLatestClocheck> VwLatestClochecks { get; set; }

    public virtual DbSet<VwPaperList> VwPaperLists { get; set; }

    public virtual DbSet<VwPaperVersionSummary> VwPaperVersionSummaries { get; set; }

    public virtual DbSet<VwRequiredCoverage> VwRequiredCoverages { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);

            entity.ToTable("ACADEMIC_SESSION");

            entity.HasIndex(e => e.SessionName, "UQ_ACADEMIC_SESSION_1").IsUnique();

            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.EndsOn).HasColumnName("ends_on");
            entity.Property(e => e.SessionName)
                .HasMaxLength(200)
                .HasColumnName("session_name");
            entity.Property(e => e.StartsOn).HasColumnName("starts_on");
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("APP_USER");

            entity.HasIndex(e => e.HomeDepartmentId, "IX_APP_USER_home_department_id");

            entity.HasIndex(e => e.ProfilePhotoFileAssetId, "IX_AppUser_Photo");

            entity.HasIndex(e => e.StaffNumber, "UQ_APP_USER_1").IsUnique();

            entity.HasIndex(e => e.Email, "UQ_APP_USER_2").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Designation)
                .HasMaxLength(100)
                .HasColumnName("designation");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.HomeDepartmentId).HasColumnName("home_department_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_APP_USER_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30)
                .HasColumnName("phone_number");
            entity.Property(e => e.ProfilePhotoFileAssetId).HasColumnName("profile_photo_file_asset_id");
            entity.Property(e => e.StaffNumber)
                .HasMaxLength(200)
                .HasColumnName("staff_number");

            entity.HasOne(d => d.HomeDepartment).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.HomeDepartmentId)
                .HasConstraintName("FK_APP_USER_home_department_id");

            entity.HasOne(d => d.ProfilePhotoFileAsset).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.ProfilePhotoFileAssetId)
                .HasConstraintName("FK_AppUser_ProfilePhoto");
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("AUDIT_EVENT");

            entity.HasIndex(e => e.ActorUserId, "IX_AUDIT_EVENT_actor_user_id");

            entity.HasIndex(e => e.DelegationId, "IX_AUDIT_EVENT_delegation_id");

            entity.Property(e => e.AuditEventId).HasColumnName("audit_event_id");
            entity.Property(e => e.Action)
                .HasMaxLength(40)
                .HasColumnName("action");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.AfterData).HasColumnName("after_data");
            entity.Property(e => e.BeforeData).HasColumnName("before_data");
            entity.Property(e => e.DelegationId).HasColumnName("delegation_id");
            entity.Property(e => e.EntityKey)
                .HasMaxLength(200)
                .HasColumnName("entity_key");
            entity.Property(e => e.EntityType)
                .HasMaxLength(40)
                .HasColumnName("entity_type");
            entity.Property(e => e.OccurredAt)
                .HasPrecision(0)
                .HasColumnName("occurred_at");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditEvents)
                .HasForeignKey(d => d.ActorUserId)
                .HasConstraintName("FK_AUDIT_EVENT_actor_user_id");

            entity.HasOne(d => d.Delegation).WithMany(p => p.AuditEvents)
                .HasForeignKey(d => d.DelegationId)
                .HasConstraintName("FK_AUDIT_EVENT_delegation_id");
        });

        modelBuilder.Entity<Clo>(entity =>
        {
            entity.ToTable("CLO");

            entity.HasIndex(e => e.OutlineId, "IX_CLO_outline_id");

            entity.HasIndex(e => new { e.OutlineId, e.CloCode }, "UQ_CLO_1").IsUnique();

            entity.Property(e => e.CloId).HasColumnName("clo_id");
            entity.Property(e => e.CloCode)
                .HasMaxLength(40)
                .HasColumnName("clo_code");
            entity.Property(e => e.CloStatement).HasColumnName("clo_statement");
            entity.Property(e => e.OutlineId).HasColumnName("outline_id");

            entity.HasOne(d => d.Outline).WithMany(p => p.Clos)
                .HasForeignKey(d => d.OutlineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CLO_outline_id");
        });

        modelBuilder.Entity<CloMapping>(entity =>
        {
            entity.HasKey(e => e.MappingId).HasName("PK__CLO_MAPP__5AE90045492B0B50");

            entity.ToTable("CLO_MAPPING");

            entity.HasIndex(e => new { e.QuestionVersionId, e.QuestionPartId, e.ExamCloId }, "UQ__CLO_MAPP__FEF02A3E3CE7E8CA").IsUnique();

            entity.Property(e => e.MappingId).HasColumnName("mapping_id");
            entity.Property(e => e.ExamCloId).HasColumnName("exam_clo_id");
            entity.Property(e => e.MappedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("mapped_at");
            entity.Property(e => e.QuestionPartId).HasColumnName("question_part_id");
            entity.Property(e => e.QuestionVersionId).HasColumnName("question_version_id");

            entity.HasOne(d => d.ExamClo).WithMany(p => p.CloMappings)
                .HasForeignKey(d => d.ExamCloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mapping_ExamCLO");

            entity.HasOne(d => d.QuestionVersion).WithMany(p => p.CloMappings)
                .HasForeignKey(d => d.QuestionVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mapping_Revision");

            entity.HasOne(d => d.QuestionPart).WithMany(p => p.CloMappings)
                .HasPrincipalKey(p => new { p.QuestionPartId, p.QuestionVersionId })
                .HasForeignKey(d => new { d.QuestionPartId, d.QuestionVersionId })
                .HasConstraintName("FK_Mapping_Part_Revision");
        });

        modelBuilder.Entity<CloMappingCheck>(entity =>
        {
            entity.HasKey(e => e.CheckId).HasName("PK__CLO_MAPP__C0EB8718F3E9B572");

            entity.ToTable("CLO_MAPPING_CHECK");

            entity.HasIndex(e => new { e.MappingId, e.CheckId }, "IX_Mapping_Check_History");

            entity.Property(e => e.CheckId).HasColumnName("check_id");
            entity.Property(e => e.CheckStatus)
                .HasMaxLength(30)
                .HasColumnName("check_status");
            entity.Property(e => e.CheckedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("checked_at");
            entity.Property(e => e.CheckerVersion)
                .HasMaxLength(100)
                .HasColumnName("checker_version");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.MappingId).HasColumnName("mapping_id");

            entity.HasOne(d => d.Mapping).WithMany(p => p.CloMappingChecks)
                .HasForeignKey(d => d.MappingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CLO_MAPPI__mappi__6D9742D9");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("COMMENT");

            entity.HasIndex(e => e.AcknowledgedByUserId, "IX_COMMENT_acknowledged_by_user_id");

            entity.HasIndex(e => e.AuthorUserId, "IX_COMMENT_author_user_id");

            entity.HasIndex(e => e.PaperItemId, "IX_COMMENT_paper_item_id");

            entity.HasIndex(e => e.PaperVersionId, "IX_COMMENT_paper_version_id");

            entity.HasIndex(e => e.ParentCommentId, "IX_COMMENT_parent_comment_id");

            entity.Property(e => e.CommentId).HasColumnName("comment_id");
            entity.Property(e => e.AcknowledgedAt)
                .HasPrecision(0)
                .HasColumnName("acknowledged_at");
            entity.Property(e => e.AcknowledgedByUserId).HasColumnName("acknowledged_by_user_id");
            entity.Property(e => e.AuthorUserId).HasColumnName("author_user_id");
            entity.Property(e => e.CommentText).HasColumnName("comment_text");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.PaperItemId).HasColumnName("paper_item_id");
            entity.Property(e => e.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(e => e.ParentCommentId).HasColumnName("parent_comment_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(40)
                .HasColumnName("target_type");

            entity.HasOne(d => d.AcknowledgedByUser).WithMany(p => p.CommentAcknowledgedByUsers)
                .HasForeignKey(d => d.AcknowledgedByUserId)
                .HasConstraintName("FK_COMMENT_acknowledged_by_user_id");

            entity.HasOne(d => d.AuthorUser).WithMany(p => p.CommentAuthorUsers)
                .HasForeignKey(d => d.AuthorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COMMENT_author_user_id");

            entity.HasOne(d => d.PaperItem).WithMany(p => p.CommentPaperItems)
                .HasForeignKey(d => d.PaperItemId)
                .HasConstraintName("FK_COMMENT_paper_item_id");

            entity.HasOne(d => d.PaperVersion).WithMany(p => p.Comments)
                .HasForeignKey(d => d.PaperVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COMMENT_paper_version_id");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .HasConstraintName("FK_COMMENT_parent_comment_id");

            entity.HasOne(d => d.PaperItemNavigation).WithMany(p => p.CommentPaperItemNavigations)
                .HasPrincipalKey(p => new { p.PaperItemId, p.PaperVersionId })
                .HasForeignKey(d => new { d.PaperItemId, d.PaperVersionId })
                .HasConstraintName("FK_Comment_ItemVersion");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("COURSE");

            entity.HasIndex(e => e.OwnerDepartmentId, "IX_COURSE_owner_department_id");

            entity.HasIndex(e => e.CourseCode, "UQ_COURSE_1").IsUnique();

            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CourseCode)
                .HasMaxLength(40)
                .HasColumnName("course_code");
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(200)
                .HasColumnName("course_title");
            entity.Property(e => e.OwnerDepartmentId).HasColumnName("owner_department_id");

            entity.HasOne(d => d.OwnerDepartment).WithMany(p => p.Courses)
                .HasForeignKey(d => d.OwnerDepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_owner_department_id");
        });

        modelBuilder.Entity<CourseExam>(entity =>
        {
            entity.ToTable("COURSE_EXAM");

            entity.HasIndex(e => e.ConfiguredByUserId, "IX_COURSE_EXAM_configured_by_user_id");

            entity.HasIndex(e => e.ExamCycleId, "IX_COURSE_EXAM_exam_cycle_id");

            entity.HasIndex(e => e.OfferingId, "IX_COURSE_EXAM_offering_id");

            entity.HasIndex(e => new { e.OfferingId, e.ExamCycleId }, "UQ_COURSE_EXAM_1").IsUnique();

            entity.HasIndex(e => new { e.CourseExamId, e.ExamCycleId }, "UQ_COURSE_EXAM_Cycle").IsUnique();

            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.ConfiguredByUserId).HasColumnName("configured_by_user_id");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.ExamCycleId).HasColumnName("exam_cycle_id");
            entity.Property(e => e.OfferingId).HasColumnName("offering_id");
            entity.Property(e => e.RequiredMarks)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("required_marks");
            entity.Property(e => e.ScopeLockedAt)
                .HasPrecision(0)
                .HasColumnName("scope_locked_at");

            entity.HasOne(d => d.ConfiguredByUser).WithMany(p => p.CourseExams)
                .HasForeignKey(d => d.ConfiguredByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_EXAM_configured_by_user_id");

            entity.HasOne(d => d.ExamCycle).WithMany(p => p.CourseExams)
                .HasForeignKey(d => d.ExamCycleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_EXAM_exam_cycle_id");

            entity.HasOne(d => d.Offering).WithMany(p => p.CourseExams)
                .HasForeignKey(d => d.OfferingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_EXAM_offering_id");
        });

        modelBuilder.Entity<CourseOffering>(entity =>
        {
            entity.HasKey(e => e.OfferingId);

            entity.ToTable("COURSE_OFFERING");

            entity.HasIndex(e => e.CourseId, "IX_COURSE_OFFERING_course_id");

            entity.HasIndex(e => e.LeadDepartmentId, "IX_COURSE_OFFERING_lead_department_id");

            entity.HasIndex(e => e.OutlineId, "IX_COURSE_OFFERING_outline_id");

            entity.HasIndex(e => e.SessionId, "IX_COURSE_OFFERING_session_id");

            entity.HasIndex(e => new { e.CourseId, e.SessionId }, "UQ_COURSE_OFFERING_1").IsUnique();

            entity.Property(e => e.OfferingId).HasColumnName("offering_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.LeadDepartmentId).HasColumnName("lead_department_id");
            entity.Property(e => e.OutlineId).HasColumnName("outline_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseOfferings)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_OFFERING_course_id");

            entity.HasOne(d => d.LeadDepartment).WithMany(p => p.CourseOfferings)
                .HasForeignKey(d => d.LeadDepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_OFFERING_lead_department_id");

            entity.HasOne(d => d.Outline).WithMany(p => p.CourseOfferingOutlines)
                .HasForeignKey(d => d.OutlineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_OFFERING_outline_id");

            entity.HasOne(d => d.Session).WithMany(p => p.CourseOfferings)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_OFFERING_session_id");

            entity.HasOne(d => d.CourseOutline).WithMany(p => p.CourseOfferingCourseOutlines)
                .HasPrincipalKey(p => new { p.OutlineId, p.CourseId })
                .HasForeignKey(d => new { d.OutlineId, d.CourseId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Offering_OutlineCourse");
        });

        modelBuilder.Entity<CourseOutline>(entity =>
        {
            entity.HasKey(e => e.OutlineId);

            entity.ToTable("COURSE_OUTLINE");

            entity.HasIndex(e => e.CourseId, "IX_COURSE_OUTLINE_course_id");

            entity.HasIndex(e => new { e.CourseId, e.VersionNumber }, "UQ_COURSE_OUTLINE_1").IsUnique();

            entity.HasIndex(e => new { e.OutlineId, e.CourseId }, "UQ_Outline_Course").IsUnique();

            entity.Property(e => e.OutlineId).HasColumnName("outline_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.PublishedAt)
                .HasPrecision(0)
                .HasColumnName("published_at");
            entity.Property(e => e.VersionNumber).HasColumnName("version_number");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseOutlines)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_OUTLINE_course_id");
        });

        modelBuilder.Entity<Datesheet>(entity =>
        {
            entity.ToTable("DATESHEET");

            entity.HasIndex(e => e.ExamCycleId, "IX_DATESHEET_exam_cycle_id");

            entity.HasIndex(e => e.ImportBatchId, "IX_DATESHEET_import_batch_id");

            entity.HasIndex(e => e.PublishedByUserId, "IX_DATESHEET_published_by_user_id");

            entity.HasIndex(e => new { e.ExamCycleId, e.RevisionNumber }, "UQ_DATESHEET_1").IsUnique();

            entity.HasIndex(e => new { e.DatesheetId, e.ExamCycleId }, "UQ_DATESHEET_Cycle").IsUnique();

            entity.HasIndex(e => e.ExamCycleId, "UX_DATESHEET_PublishedCycle")
                .IsUnique()
                .HasFilter("([status]=N'PUBLISHED')");

            entity.Property(e => e.DatesheetId).HasColumnName("datesheet_id");
            entity.Property(e => e.ExamCycleId).HasColumnName("exam_cycle_id");
            entity.Property(e => e.ImportBatchId).HasColumnName("import_batch_id");
            entity.Property(e => e.PublishedAt)
                .HasPrecision(0)
                .HasColumnName("published_at");
            entity.Property(e => e.PublishedByUserId).HasColumnName("published_by_user_id");
            entity.Property(e => e.RevisionNumber).HasColumnName("revision_number");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT", "DF_DATESHEET_status")
                .HasColumnName("status");

            entity.HasOne(d => d.ExamCycle).WithOne(p => p.Datesheet)
                .HasForeignKey<Datesheet>(d => d.ExamCycleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DATESHEET_exam_cycle_id");

            entity.HasOne(d => d.ImportBatch).WithMany(p => p.Datesheets)
                .HasForeignKey(d => d.ImportBatchId)
                .HasConstraintName("FK_DATESHEET_import_batch_id");

            entity.HasOne(d => d.PublishedByUser).WithMany(p => p.Datesheets)
                .HasForeignKey(d => d.PublishedByUserId)
                .HasConstraintName("FK_DATESHEET_published_by_user_id");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("DEPARTMENT");

            entity.HasIndex(e => e.DepartmentCode, "UQ_DEPARTMENT_1").IsUnique();

            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.DepartmentCode)
                .HasMaxLength(40)
                .HasColumnName("department_code");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(200)
                .HasColumnName("department_name");
        });

        modelBuilder.Entity<DirectorDelegation>(entity =>
        {
            entity.HasKey(e => e.DelegationId);

            entity.ToTable("DIRECTOR_DELEGATION");

            entity.HasIndex(e => e.DelegateUserId, "IX_DIRECTOR_DELEGATION_delegate_user_id");

            entity.HasIndex(e => e.GrantorUserRoleId, "IX_DIRECTOR_DELEGATION_grantor_user_role_id");

            entity.HasIndex(e => e.RevokedByUserId, "IX_DIRECTOR_DELEGATION_revoked_by_user_id");

            entity.Property(e => e.DelegationId).HasColumnName("delegation_id");
            entity.Property(e => e.DelegateUserId).HasColumnName("delegate_user_id");
            entity.Property(e => e.EndsAt)
                .HasPrecision(0)
                .HasColumnName("ends_at");
            entity.Property(e => e.GrantorUserRoleId).HasColumnName("grantor_user_role_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RevokedAt)
                .HasPrecision(0)
                .HasColumnName("revoked_at");
            entity.Property(e => e.RevokedByUserId).HasColumnName("revoked_by_user_id");
            entity.Property(e => e.StartsAt)
                .HasPrecision(0)
                .HasColumnName("starts_at");

            entity.HasOne(d => d.DelegateUser).WithMany(p => p.DirectorDelegationDelegateUsers)
                .HasForeignKey(d => d.DelegateUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DIRECTOR_DELEGATION_delegate_user_id");

            entity.HasOne(d => d.GrantorUserRole).WithMany(p => p.DirectorDelegations)
                .HasForeignKey(d => d.GrantorUserRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DIRECTOR_DELEGATION_grantor_user_role_id");

            entity.HasOne(d => d.RevokedByUser).WithMany(p => p.DirectorDelegationRevokedByUsers)
                .HasForeignKey(d => d.RevokedByUserId)
                .HasConstraintName("FK_DIRECTOR_DELEGATION_revoked_by_user_id");
        });

        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.ErrorLogId).HasName("PK__ERROR_LO__D67FEBA3709CB65A");

            entity.ToTable("ERROR_LOG");

            entity.HasIndex(e => e.OccurredAt, "IX_ERROR_LOG_OccurredAt").IsDescending();

            entity.HasIndex(e => e.TraceId, "IX_ERROR_LOG_TraceId").HasFilter("([trace_id] IS NOT NULL)");

            entity.Property(e => e.ErrorLogId).HasColumnName("error_log_id");
            entity.Property(e => e.ErrorLine).HasColumnName("error_line");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.ErrorNumber).HasColumnName("error_number");
            entity.Property(e => e.ErrorSource)
                .HasMaxLength(30)
                .HasColumnName("error_source");
            entity.Property(e => e.OccurredAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ERROR_LOG_OccurredAt")
                .HasColumnName("occurred_at");
            entity.Property(e => e.ProcedureName)
                .HasMaxLength(256)
                .HasColumnName("procedure_name");
            entity.Property(e => e.RequestPath)
                .HasMaxLength(1000)
                .HasColumnName("request_path");
            entity.Property(e => e.StackTrace).HasColumnName("stack_trace");
            entity.Property(e => e.TraceId)
                .HasMaxLength(100)
                .HasColumnName("trace_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<ExamClo>(entity =>
        {
            entity.ToTable("EXAM_CLO");

            entity.HasIndex(e => e.CloId, "IX_EXAM_CLO_clo_id");

            entity.HasIndex(e => e.CourseExamId, "IX_EXAM_CLO_course_exam_id");

            entity.HasIndex(e => new { e.CourseExamId, e.CloId }, "UQ_EXAM_CLO_1").IsUnique();

            entity.Property(e => e.ExamCloId).HasColumnName("exam_clo_id");
            entity.Property(e => e.CloId).HasColumnName("clo_id");
            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");

            entity.HasOne(d => d.Clo).WithMany(p => p.ExamClos)
                .HasForeignKey(d => d.CloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXAM_CLO_clo_id");

            entity.HasOne(d => d.CourseExam).WithMany(p => p.ExamClos)
                .HasForeignKey(d => d.CourseExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXAM_CLO_course_exam_id");
        });

        modelBuilder.Entity<ExamCommonTopic>(entity =>
        {
            entity.HasKey(e => e.CommonTopicId).HasName("PK__EXAM_COM__F712B99E063002B0");

            entity.ToTable("EXAM_COMMON_TOPIC");

            entity.HasIndex(e => new { e.CourseExamId, e.TopicId }, "UQ__EXAM_COM__4B1648659FEA5623").IsUnique();

            entity.Property(e => e.CommonTopicId).HasColumnName("common_topic_id");
            entity.Property(e => e.ComputedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("computed_at");
            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");

            entity.HasOne(d => d.CourseExam).WithMany(p => p.ExamCommonTopics)
                .HasForeignKey(d => d.CourseExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EXAM_COMM__cours__68D28DBC");

            entity.HasOne(d => d.Topic).WithMany(p => p.ExamCommonTopics)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EXAM_COMM__topic__69C6B1F5");
        });

        modelBuilder.Entity<ExamCycle>(entity =>
        {
            entity.ToTable("EXAM_CYCLE");

            entity.HasIndex(e => e.SessionId, "IX_EXAM_CYCLE_session_id");

            entity.HasIndex(e => new { e.SessionId, e.ExamType }, "UQ_EXAM_CYCLE_1").IsUnique();

            entity.Property(e => e.ExamCycleId).HasColumnName("exam_cycle_id");
            entity.Property(e => e.ExamType)
                .HasMaxLength(40)
                .HasColumnName("exam_type");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Session).WithMany(p => p.ExamCycles)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXAM_CYCLE_session_id");
        });

        modelBuilder.Entity<ExamSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);

            entity.ToTable("EXAM_SCHEDULE");

            entity.HasIndex(e => e.CourseExamId, "IX_EXAM_SCHEDULE_course_exam_id");

            entity.HasIndex(e => e.DatesheetId, "IX_EXAM_SCHEDULE_datesheet_id");

            entity.HasIndex(e => e.ExamCycleId, "IX_EXAM_SCHEDULE_exam_cycle_id");

            entity.HasIndex(e => new { e.DatesheetId, e.CourseExamId }, "UQ_EXAM_SCHEDULE_1").IsUnique();

            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.DatesheetId).HasColumnName("datesheet_id");
            entity.Property(e => e.EndTime)
                .HasPrecision(0)
                .HasColumnName("end_time");
            entity.Property(e => e.ExamCycleId).HasColumnName("exam_cycle_id");
            entity.Property(e => e.ExamDate).HasColumnName("exam_date");
            entity.Property(e => e.StartTime)
                .HasPrecision(0)
                .HasColumnName("start_time");

            entity.HasOne(d => d.CourseExam).WithMany(p => p.ExamScheduleCourseExams)
                .HasForeignKey(d => d.CourseExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXAM_SCHEDULE_course_exam_id");

            entity.HasOne(d => d.Datesheet).WithMany(p => p.ExamScheduleDatesheets)
                .HasForeignKey(d => d.DatesheetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXAM_SCHEDULE_datesheet_id");

            entity.HasOne(d => d.CourseExamNavigation).WithMany(p => p.ExamScheduleCourseExamNavigations)
                .HasPrincipalKey(p => new { p.CourseExamId, p.ExamCycleId })
                .HasForeignKey(d => new { d.CourseExamId, d.ExamCycleId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXAM_SCHEDULE_CourseExamCycle");

            entity.HasOne(d => d.DatesheetNavigation).WithMany(p => p.ExamScheduleDatesheetNavigations)
                .HasPrincipalKey(p => new { p.DatesheetId, p.ExamCycleId })
                .HasForeignKey(d => new { d.DatesheetId, d.ExamCycleId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXAM_SCHEDULE_DatesheetCycle");
        });

        modelBuilder.Entity<FileAsset>(entity =>
        {
            entity.ToTable("FILE_ASSET");

            entity.HasIndex(e => e.UploadedByUserId, "IX_FILE_ASSET_uploaded_by_user_id");

            entity.HasIndex(e => e.StorageKey, "UQ_FILE_ASSET_1").IsUnique();

            entity.Property(e => e.FileAssetId).HasColumnName("file_asset_id");
            entity.Property(e => e.Checksum)
                .HasMaxLength(256)
                .HasColumnName("checksum");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.MimeType)
                .HasMaxLength(128)
                .HasColumnName("mime_type");
            entity.Property(e => e.OriginalName)
                .HasMaxLength(256)
                .HasColumnName("original_name");
            entity.Property(e => e.StorageKey)
                .HasMaxLength(256)
                .HasColumnName("storage_key");
            entity.Property(e => e.UploadedByUserId).HasColumnName("uploaded_by_user_id");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.FileAssets)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FILE_ASSET_uploaded_by_user_id");
        });

        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.ToTable("IMPORT_BATCH");

            entity.HasIndex(e => e.FileAssetId, "IX_IMPORT_BATCH_file_asset_id");

            entity.HasIndex(e => e.ImportedByUserId, "IX_IMPORT_BATCH_imported_by_user_id");

            entity.Property(e => e.ImportBatchId).HasColumnName("import_batch_id");
            entity.Property(e => e.CompletedAt)
                .HasPrecision(0)
                .HasColumnName("completed_at");
            entity.Property(e => e.FileAssetId).HasColumnName("file_asset_id");
            entity.Property(e => e.ImportStatus)
                .HasMaxLength(40)
                .HasColumnName("import_status");
            entity.Property(e => e.ImportedByUserId).HasColumnName("imported_by_user_id");
            entity.Property(e => e.RowsAdded).HasColumnName("rows_added");
            entity.Property(e => e.RowsFailed).HasColumnName("rows_failed");
            entity.Property(e => e.RowsUpdated).HasColumnName("rows_updated");
            entity.Property(e => e.StartedAt)
                .HasPrecision(0)
                .HasColumnName("started_at");

            entity.HasOne(d => d.FileAsset).WithMany(p => p.ImportBatches)
                .HasForeignKey(d => d.FileAssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IMPORT_BATCH_file_asset_id");

            entity.HasOne(d => d.ImportedByUser).WithMany(p => p.ImportBatches)
                .HasForeignKey(d => d.ImportedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IMPORT_BATCH_imported_by_user_id");
        });

        modelBuilder.Entity<ImportError>(entity =>
        {
            entity.ToTable("IMPORT_ERROR");

            entity.HasIndex(e => e.ImportBatchId, "IX_IMPORT_ERROR_import_batch_id");

            entity.Property(e => e.ImportErrorId).HasColumnName("import_error_id");
            entity.Property(e => e.ColumnName)
                .HasMaxLength(200)
                .HasColumnName("column_name");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.ImportBatchId).HasColumnName("import_batch_id");
            entity.Property(e => e.RowNumber).HasColumnName("row_number");
            entity.Property(e => e.SheetName)
                .HasMaxLength(200)
                .HasColumnName("sheet_name");

            entity.HasOne(d => d.ImportBatch).WithMany(p => p.ImportErrors)
                .HasForeignKey(d => d.ImportBatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IMPORT_ERROR_import_batch_id");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("NOTIFICATION");

            entity.HasIndex(e => e.DelegationId, "IX_NOTIFICATION_delegation_id");

            entity.HasIndex(e => e.PaperId, "IX_NOTIFICATION_paper_id");

            entity.HasIndex(e => e.RecipientUserId, "IX_NOTIFICATION_recipient_user_id");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.DelegationId).HasColumnName("delegation_id");
            entity.Property(e => e.EventType)
                .HasMaxLength(40)
                .HasColumnName("event_type");
            entity.Property(e => e.MessageText).HasColumnName("message_text");
            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.ReadAt)
                .HasPrecision(0)
                .HasColumnName("read_at");
            entity.Property(e => e.RecipientUserId).HasColumnName("recipient_user_id");

            entity.HasOne(d => d.Delegation).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.DelegationId)
                .HasConstraintName("FK_NOTIFICATION_delegation_id");

            entity.HasOne(d => d.Paper).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.PaperId)
                .HasConstraintName("FK_NOTIFICATION_paper_id");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.RecipientUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NOTIFICATION_recipient_user_id");
        });

        modelBuilder.Entity<OfferingSection>(entity =>
        {
            entity.ToTable("OFFERING_SECTION");

            entity.HasIndex(e => e.OfferingId, "IX_OFFERING_SECTION_offering_id");

            entity.HasIndex(e => e.SectionId, "IX_OFFERING_SECTION_section_id");

            entity.HasIndex(e => new { e.OfferingId, e.SectionId }, "UQ_OFFERING_SECTION_1").IsUnique();

            entity.Property(e => e.OfferingSectionId).HasColumnName("offering_section_id");
            entity.Property(e => e.OfferingId).HasColumnName("offering_id");
            entity.Property(e => e.SectionId).HasColumnName("section_id");

            entity.HasOne(d => d.Offering).WithMany(p => p.OfferingSections)
                .HasForeignKey(d => d.OfferingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OFFERING_SECTION_offering_id");

            entity.HasOne(d => d.Section).WithMany(p => p.OfferingSections)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OFFERING_SECTION_section_id");
        });

        modelBuilder.Entity<Paper>(entity =>
        {
            entity.ToTable("PAPER");

            entity.HasIndex(e => e.DeletedByUserId, "IX_PAPER_deleted_by_user_id");

            entity.HasIndex(e => e.CourseExamId, "UQ_PAPER_1").IsUnique();

            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.ArchivedAt)
                .HasPrecision(0)
                .HasColumnName("archived_at");
            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.DeletedAt)
                .HasPrecision(0)
                .HasColumnName("deleted_at");
            entity.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id");
            entity.Property(e => e.WorkflowStatus)
                .HasMaxLength(40)
                .HasColumnName("workflow_status");

            entity.HasOne(d => d.CourseExam).WithOne(p => p.Paper)
                .HasForeignKey<Paper>(d => d.CourseExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_course_exam_id");

            entity.HasOne(d => d.DeletedByUser).WithMany(p => p.Papers)
                .HasForeignKey(d => d.DeletedByUserId)
                .HasConstraintName("FK_PAPER_deleted_by_user_id");
        });

        modelBuilder.Entity<PaperAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId);

            entity.ToTable("PAPER_ASSIGNMENT");

            entity.HasIndex(e => e.AssignedByUserRoleId, "IX_PAPER_ASSIGNMENT_assigned_by_user_role_id");

            entity.HasIndex(e => e.PaperId, "IX_PAPER_ASSIGNMENT_paper_id");

            entity.HasIndex(e => e.SetterUserId, "IX_PAPER_ASSIGNMENT_setter_user_id");

            entity.HasIndex(e => e.PaperId, "UX_PAPER_ASSIGNMENT_Current")
                .IsUnique()
                .HasFilter("([ended_at] IS NULL)");

            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.AssignedAt)
                .HasPrecision(0)
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedByUserRoleId).HasColumnName("assigned_by_user_role_id");
            entity.Property(e => e.DueAt)
                .HasPrecision(0)
                .HasColumnName("due_at");
            entity.Property(e => e.EndedAt)
                .HasPrecision(0)
                .HasColumnName("ended_at");
            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.SetterUserId).HasColumnName("setter_user_id");

            entity.HasOne(d => d.AssignedByUserRole).WithMany(p => p.PaperAssignments)
                .HasForeignKey(d => d.AssignedByUserRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_ASSIGNMENT_assigned_by_user_role_id");

            entity.HasOne(d => d.Paper).WithOne(p => p.PaperAssignment)
                .HasForeignKey<PaperAssignment>(d => d.PaperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_ASSIGNMENT_paper_id");

            entity.HasOne(d => d.SetterUser).WithMany(p => p.PaperAssignments)
                .HasForeignKey(d => d.SetterUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_ASSIGNMENT_setter_user_id");
        });

        modelBuilder.Entity<PaperItem>(entity =>
        {
            entity.ToTable("PAPER_ITEM");

            entity.HasIndex(e => e.PaperVersionId, "IX_PAPER_ITEM_paper_version_id");

            entity.HasIndex(e => e.QuestionVersionId, "IX_PAPER_ITEM_question_version_id");

            entity.HasIndex(e => new { e.PaperVersionId, e.Placement, e.SortOrder }, "UQ_PAPER_ITEM_1").IsUnique();

            entity.HasIndex(e => new { e.PaperVersionId, e.QuestionVersionId }, "UQ_PAPER_ITEM_2").IsUnique();

            entity.HasIndex(e => new { e.PaperItemId, e.PaperVersionId }, "UQ_PaperItem_Version").IsUnique();

            entity.Property(e => e.PaperItemId).HasColumnName("paper_item_id");
            entity.Property(e => e.DisplayNumber)
                .HasMaxLength(200)
                .HasColumnName("display_number");
            entity.Property(e => e.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(e => e.Placement)
                .HasMaxLength(40)
                .HasColumnName("placement");
            entity.Property(e => e.QuestionVersionId).HasColumnName("question_version_id");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.PaperVersion).WithMany(p => p.PaperItems)
                .HasForeignKey(d => d.PaperVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_ITEM_paper_version_id");

            entity.HasOne(d => d.QuestionVersion).WithMany(p => p.PaperItems)
                .HasForeignKey(d => d.QuestionVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_ITEM_question_version_id");
        });

        modelBuilder.Entity<PaperMatch>(entity =>
        {
            entity.ToTable("PAPER_MATCH");

            entity.HasIndex(e => e.SimilarityRunId, "IX_PAPER_MATCH_similarity_run_id");

            entity.HasIndex(e => e.SourcePaperVersionId, "IX_PAPER_MATCH_source_paper_version_id");

            entity.HasIndex(e => new { e.SimilarityRunId, e.SourcePaperVersionId }, "UQ_PAPER_MATCH_1").IsUnique();

            entity.Property(e => e.PaperMatchId).HasColumnName("paper_match_id");
            entity.Property(e => e.SimilarityPercent)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("similarity_percent");
            entity.Property(e => e.SimilarityRunId).HasColumnName("similarity_run_id");
            entity.Property(e => e.SourcePaperVersionId).HasColumnName("source_paper_version_id");

            entity.HasOne(d => d.SimilarityRun).WithMany(p => p.PaperMatches)
                .HasForeignKey(d => d.SimilarityRunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_MATCH_similarity_run_id");

            entity.HasOne(d => d.SourcePaperVersion).WithMany(p => p.PaperMatches)
                .HasForeignKey(d => d.SourcePaperVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_MATCH_source_paper_version_id");
        });

        modelBuilder.Entity<PaperParticipant>(entity =>
        {
            entity.HasKey(e => new { e.PaperId, e.TeacherUserId });

            entity.ToTable("PAPER_PARTICIPANT");

            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id");
            entity.Property(e => e.JoinedAt)
                .HasPrecision(0)
                .HasColumnName("joined_at");
            entity.Property(e => e.LeftAt)
                .HasPrecision(0)
                .HasColumnName("left_at");

            entity.HasOne(d => d.Paper).WithMany(p => p.PaperParticipants)
                .HasForeignKey(d => d.PaperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_PARTICIPANT_paper_id");

            entity.HasOne(d => d.TeacherUser).WithMany(p => p.PaperParticipants)
                .HasForeignKey(d => d.TeacherUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_PARTICIPANT_teacher_user_id");
        });

        modelBuilder.Entity<PaperSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId);

            entity.ToTable("PAPER_SUBMISSION");

            entity.HasIndex(e => e.SubmittedByUserId, "IX_PAPER_SUBMISSION_submitted_by_user_id");

            entity.HasIndex(e => e.PaperVersionId, "UQ_PAPER_SUBMISSION_1").IsUnique();

            entity.HasIndex(e => new { e.SubmissionId, e.PaperVersionId }, "UQ_Submission_Version").IsUnique();

            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(e => e.SubmissionNumber).HasColumnName("submission_number");
            entity.Property(e => e.SubmittedAt)
                .HasPrecision(0)
                .HasColumnName("submitted_at");
            entity.Property(e => e.SubmittedByUserId).HasColumnName("submitted_by_user_id");

            entity.HasOne(d => d.PaperVersion).WithOne(p => p.PaperSubmission)
                .HasForeignKey<PaperSubmission>(d => d.PaperVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_SUBMISSION_paper_version_id");

            entity.HasOne(d => d.SubmittedByUser).WithMany(p => p.PaperSubmissions)
                .HasForeignKey(d => d.SubmittedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_SUBMISSION_submitted_by_user_id");
        });

        modelBuilder.Entity<PaperTopicCheck>(entity =>
        {
            entity.HasKey(e => e.TopicCheckId).HasName("PK__PAPER_TO__5136DCA7E3C9618C");

            entity.ToTable("PAPER_TOPIC_CHECK");

            entity.Property(e => e.TopicCheckId).HasColumnName("topic_check_id");
            entity.Property(e => e.CheckStatus)
                .HasMaxLength(20)
                .HasColumnName("check_status");
            entity.Property(e => e.CheckedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("checked_at");
            entity.Property(e => e.CommonTopicId).HasColumnName("common_topic_id");
            entity.Property(e => e.Explanation)
                .HasMaxLength(1000)
                .HasColumnName("explanation");
            entity.Property(e => e.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(e => e.QuestionPartId).HasColumnName("question_part_id");

            entity.HasOne(d => d.CommonTopic).WithMany(p => p.PaperTopicChecks)
                .HasForeignKey(d => d.CommonTopicId)
                .HasConstraintName("FK__PAPER_TOP__commo__74444068");

            entity.HasOne(d => d.PaperVersion).WithMany(p => p.PaperTopicChecks)
                .HasForeignKey(d => d.PaperVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PAPER_TOP__paper__725BF7F6");

            entity.HasOne(d => d.QuestionPart).WithMany(p => p.PaperTopicChecks)
                .HasForeignKey(d => d.QuestionPartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PAPER_TOP__quest__73501C2F");
        });

        modelBuilder.Entity<PaperVersion>(entity =>
        {
            entity.ToTable("PAPER_VERSION");

            entity.HasIndex(e => e.BasedOnVersionId, "IX_PAPER_VERSION_based_on_version_id");

            entity.HasIndex(e => e.CopiedFromVersionId, "IX_PAPER_VERSION_copied_from_version_id");

            entity.HasIndex(e => e.CreatedByUserId, "IX_PAPER_VERSION_created_by_user_id");

            entity.HasIndex(e => e.PaperId, "IX_PAPER_VERSION_paper_id");

            entity.HasIndex(e => new { e.PaperId, e.RevisionNumber }, "UQ_PAPER_VERSION_1").IsUnique();

            entity.HasIndex(e => new { e.PaperVersionId, e.PaperId }, "UQ_PaperVersion_Paper").IsUnique();

            entity.Property(e => e.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(e => e.BasedOnVersionId).HasColumnName("based_on_version_id");
            entity.Property(e => e.ContentHash)
                .HasMaxLength(256)
                .HasColumnName("content_hash");
            entity.Property(e => e.CopiedFromVersionId).HasColumnName("copied_from_version_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.DurationMinutesSnapshot).HasColumnName("duration_minutes_snapshot");
            entity.Property(e => e.FrozenAt)
                .HasPrecision(0)
                .HasColumnName("frozen_at");
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.RequiredMarksSnapshot)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("required_marks_snapshot");
            entity.Property(e => e.RevisionNumber).HasColumnName("revision_number");
            entity.Property(e => e.RevisionReason)
                .HasMaxLength(500)
                .HasColumnName("revision_reason");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");

            entity.HasOne(d => d.BasedOnVersion).WithMany(p => p.InverseBasedOnVersion)
                .HasForeignKey(d => d.BasedOnVersionId)
                .HasConstraintName("FK_PAPER_VERSION_based_on_version_id");

            entity.HasOne(d => d.CopiedFromVersion).WithMany(p => p.InverseCopiedFromVersion)
                .HasForeignKey(d => d.CopiedFromVersionId)
                .HasConstraintName("FK_PAPER_VERSION_copied_from_version_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.PaperVersions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_VERSION_created_by_user_id");

            entity.HasOne(d => d.Paper).WithMany(p => p.PaperVersions)
                .HasForeignKey(d => d.PaperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAPER_VERSION_paper_id");

            entity.HasOne(d => d.PaperVersionNavigation).WithMany(p => p.InversePaperVersionNavigation)
                .HasPrincipalKey(p => new { p.PaperVersionId, p.PaperId })
                .HasForeignKey(d => new { d.BasedOnVersionId, d.PaperId })
                .HasConstraintName("FK_PaperVersion_BasedOnPaper");
        });

        modelBuilder.Entity<PaperVersionCommonTopic>(entity =>
        {
            entity.HasKey(e => new { e.PaperVersionId, e.TopicId });

            entity.ToTable("PAPER_VERSION_COMMON_TOPIC");

            entity.Property(e => e.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");
            entity.Property(e => e.TopicTitleSnapshot)
                .HasMaxLength(200)
                .HasColumnName("topic_title_snapshot");

            entity.HasOne(d => d.PaperVersion).WithMany(p => p.PaperVersionCommonTopics)
                .HasForeignKey(d => d.PaperVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaperVersionTopic_Version");

            entity.HasOne(d => d.Topic).WithMany(p => p.PaperVersionCommonTopics)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaperVersionTopic_Topic");
        });

        modelBuilder.Entity<PhysicalReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId);

            entity.ToTable("PHYSICAL_RECEIPT");

            entity.HasIndex(e => e.DeliveredByUserId, "IX_PHYSICAL_RECEIPT_delivered_by_user_id");

            entity.HasIndex(e => e.ReceivedByUserId, "IX_PHYSICAL_RECEIPT_received_by_user_id");

            entity.HasIndex(e => e.ApprovalReviewId, "UQ_PHYSICAL_RECEIPT_1").IsUnique();

            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.ApprovalReviewId).HasColumnName("approval_review_id");
            entity.Property(e => e.DeliveredByUserId).HasColumnName("delivered_by_user_id");
            entity.Property(e => e.ReceivedAt)
                .HasPrecision(0)
                .HasColumnName("received_at");
            entity.Property(e => e.ReceivedByUserId).HasColumnName("received_by_user_id");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.SealStatus)
                .HasMaxLength(40)
                .HasColumnName("seal_status");

            entity.HasOne(d => d.ApprovalReview).WithOne(p => p.PhysicalReceipt)
                .HasForeignKey<PhysicalReceipt>(d => d.ApprovalReviewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PHYSICAL_RECEIPT_approval_review_id");

            entity.HasOne(d => d.DeliveredByUser).WithMany(p => p.PhysicalReceiptDeliveredByUsers)
                .HasForeignKey(d => d.DeliveredByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PHYSICAL_RECEIPT_delivered_by_user_id");

            entity.HasOne(d => d.ReceivedByUser).WithMany(p => p.PhysicalReceiptReceivedByUsers)
                .HasForeignKey(d => d.ReceivedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PHYSICAL_RECEIPT_received_by_user_id");
        });

        modelBuilder.Entity<PrintExport>(entity =>
        {
            entity.ToTable("PRINT_EXPORT");

            entity.HasIndex(e => e.ApprovalReviewId, "IX_PRINT_EXPORT_approval_review_id");

            entity.HasIndex(e => e.FileAssetId, "IX_PRINT_EXPORT_file_asset_id");

            entity.HasIndex(e => e.RequestedByUserId, "IX_PRINT_EXPORT_requested_by_user_id");

            entity.Property(e => e.PrintExportId).HasColumnName("print_export_id");
            entity.Property(e => e.ApprovalReviewId).HasColumnName("approval_review_id");
            entity.Property(e => e.FileAssetId).HasColumnName("file_asset_id");
            entity.Property(e => e.GeneratedAt)
                .HasPrecision(0)
                .HasColumnName("generated_at");
            entity.Property(e => e.RequestedByUserId).HasColumnName("requested_by_user_id");

            entity.HasOne(d => d.ApprovalReview).WithMany(p => p.PrintExports)
                .HasForeignKey(d => d.ApprovalReviewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PRINT_EXPORT_approval_review_id");

            entity.HasOne(d => d.FileAsset).WithMany(p => p.PrintExports)
                .HasForeignKey(d => d.FileAssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PRINT_EXPORT_file_asset_id");

            entity.HasOne(d => d.RequestedByUser).WithMany(p => p.PrintExports)
                .HasForeignKey(d => d.RequestedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PRINT_EXPORT_requested_by_user_id");
        });

        modelBuilder.Entity<BIITPaperSettingAPI.Models.Program>(entity =>
        {
            entity.ToTable("PROGRAM");

            entity.HasIndex(e => e.DepartmentId, "IX_PROGRAM_department_id");

            entity.HasIndex(e => e.ProgramCode, "UQ_PROGRAM_1").IsUnique();

            entity.Property(e => e.ProgramId).HasColumnName("program_id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.ProgramCode)
                .HasMaxLength(40)
                .HasColumnName("program_code");
            entity.Property(e => e.ProgramName)
                .HasMaxLength(200)
                .HasColumnName("program_name");

            entity.HasOne(d => d.Department).WithMany(p => p.Programs)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PROGRAM_department_id");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("QUESTION");

            entity.HasIndex(e => e.AuthorUserId, "IX_QUESTION_author_user_id");

            entity.HasIndex(e => e.DeletedByUserId, "IX_QUESTION_deleted_by_user_id");

            entity.HasIndex(e => e.PaperId, "IX_QUESTION_paper_id");

            entity.HasIndex(e => e.SourcePaperItemId, "IX_QUESTION_source_paper_item_id");

            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.AuthorUserId).HasColumnName("author_user_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasPrecision(0)
                .HasColumnName("deleted_at");
            entity.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id");
            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.SourcePaperItemId).HasColumnName("source_paper_item_id");

            entity.HasOne(d => d.AuthorUser).WithMany(p => p.QuestionAuthorUsers)
                .HasForeignKey(d => d.AuthorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_author_user_id");

            entity.HasOne(d => d.DeletedByUser).WithMany(p => p.QuestionDeletedByUsers)
                .HasForeignKey(d => d.DeletedByUserId)
                .HasConstraintName("FK_QUESTION_deleted_by_user_id");

            entity.HasOne(d => d.Paper).WithMany(p => p.Questions)
                .HasForeignKey(d => d.PaperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_paper_id");

            entity.HasOne(d => d.SourcePaperItem).WithMany(p => p.Questions)
                .HasForeignKey(d => d.SourcePaperItemId)
                .HasConstraintName("FK_QUESTION_source_paper_item_id");
        });

        modelBuilder.Entity<QuestionAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId);

            entity.ToTable("QUESTION_ATTACHMENT");

            entity.HasIndex(e => e.FileAssetId, "IX_QUESTION_ATTACHMENT_file_asset_id");

            entity.HasIndex(e => e.QuestionPartId, "IX_QUESTION_ATTACHMENT_question_part_id");

            entity.HasIndex(e => e.QuestionVersionId, "IX_QUESTION_ATTACHMENT_question_version_id");

            entity.Property(e => e.AttachmentId).HasColumnName("attachment_id");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.FileAssetId).HasColumnName("file_asset_id");
            entity.Property(e => e.QuestionPartId).HasColumnName("question_part_id");
            entity.Property(e => e.QuestionVersionId).HasColumnName("question_version_id");

            entity.HasOne(d => d.FileAsset).WithMany(p => p.QuestionAttachments)
                .HasForeignKey(d => d.FileAssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_ATTACHMENT_file_asset_id");

            entity.HasOne(d => d.QuestionPart).WithMany(p => p.QuestionAttachmentQuestionParts)
                .HasForeignKey(d => d.QuestionPartId)
                .HasConstraintName("FK_QUESTION_ATTACHMENT_question_part_id");

            entity.HasOne(d => d.QuestionVersion).WithMany(p => p.QuestionAttachments)
                .HasForeignKey(d => d.QuestionVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_ATTACHMENT_question_version_id");

            entity.HasOne(d => d.QuestionPartNavigation).WithMany(p => p.QuestionAttachmentQuestionPartNavigations)
                .HasPrincipalKey(p => new { p.QuestionPartId, p.QuestionVersionId })
                .HasForeignKey(d => new { d.QuestionPartId, d.QuestionVersionId })
                .HasConstraintName("FK_Attachment_PartVersion");
        });

        modelBuilder.Entity<QuestionMatch>(entity =>
        {
            entity.ToTable("QUESTION_MATCH");

            entity.HasIndex(e => e.CurrentItemId, "IX_QUESTION_MATCH_current_item_id");

            entity.HasIndex(e => e.SimilarityRunId, "IX_QUESTION_MATCH_similarity_run_id");

            entity.HasIndex(e => e.SourceItemId, "IX_QUESTION_MATCH_source_item_id");

            entity.HasIndex(e => new { e.SimilarityRunId, e.CurrentItemId, e.SourceItemId }, "UQ_QUESTION_MATCH_1").IsUnique();

            entity.Property(e => e.QuestionMatchId).HasColumnName("question_match_id");
            entity.Property(e => e.CurrentItemId).HasColumnName("current_item_id");
            entity.Property(e => e.SimilarityPercent)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("similarity_percent");
            entity.Property(e => e.SimilarityRunId).HasColumnName("similarity_run_id");
            entity.Property(e => e.SourceItemId).HasColumnName("source_item_id");

            entity.HasOne(d => d.CurrentItem).WithMany(p => p.QuestionMatchCurrentItems)
                .HasForeignKey(d => d.CurrentItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_MATCH_current_item_id");

            entity.HasOne(d => d.SimilarityRun).WithMany(p => p.QuestionMatches)
                .HasForeignKey(d => d.SimilarityRunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_MATCH_similarity_run_id");

            entity.HasOne(d => d.SourceItem).WithMany(p => p.QuestionMatchSourceItems)
                .HasForeignKey(d => d.SourceItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_MATCH_source_item_id");
        });

        modelBuilder.Entity<QuestionPart>(entity =>
        {
            entity.ToTable("QUESTION_PART");

            entity.HasIndex(e => new { e.ParentPartId, e.QuestionVersionId }, "IX_Part_Parent");

            entity.HasIndex(e => e.QuestionVersionId, "IX_QUESTION_PART_question_version_id");

            entity.HasIndex(e => e.TopicId, "IX_QUESTION_PART_topic_id");

            entity.HasIndex(e => new { e.QuestionPartId, e.QuestionVersionId }, "UQ_Part_Revision").IsUnique();

            entity.HasIndex(e => new { e.QuestionVersionId, e.ParentPartId, e.PartOrder }, "UQ_QUESTION_PART_1").IsUnique();

            entity.Property(e => e.QuestionPartId).HasColumnName("question_part_id");
            entity.Property(e => e.Difficulty)
                .HasMaxLength(40)
                .HasColumnName("difficulty");
            entity.Property(e => e.Marks)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("marks");
            entity.Property(e => e.ParentPartId).HasColumnName("parent_part_id");
            entity.Property(e => e.PartKind)
                .HasMaxLength(10)
                .HasDefaultValue("ITEM", "DF_QuestionPart_Kind")
                .HasColumnName("part_kind");
            entity.Property(e => e.PartLabel)
                .HasMaxLength(200)
                .HasColumnName("part_label");
            entity.Property(e => e.PartOrder).HasColumnName("part_order");
            entity.Property(e => e.PartText).HasColumnName("part_text");
            entity.Property(e => e.QuestionVersionId).HasColumnName("question_version_id");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");

            entity.HasOne(d => d.QuestionVersion).WithMany(p => p.QuestionParts)
                .HasForeignKey(d => d.QuestionVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_PART_question_version_id");

            entity.HasOne(d => d.Topic).WithMany(p => p.QuestionParts)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK_QUESTION_PART_topic_id");

            entity.HasOne(d => d.QuestionPartNavigation).WithMany(p => p.InverseQuestionPartNavigation)
                .HasPrincipalKey(p => new { p.QuestionPartId, p.QuestionVersionId })
                .HasForeignKey(d => new { d.ParentPartId, d.QuestionVersionId })
                .HasConstraintName("FK_QuestionPart_ParentVersion");
        });

        modelBuilder.Entity<QuestionVersion>(entity =>
        {
            entity.ToTable("QUESTION_VERSION");

            entity.HasIndex(e => e.CreatedByUserId, "IX_QUESTION_VERSION_created_by_user_id");

            entity.HasIndex(e => e.QuestionId, "IX_QUESTION_VERSION_question_id");

            entity.HasIndex(e => new { e.QuestionId, e.RevisionNumber }, "UQ_QUESTION_VERSION_1").IsUnique();

            entity.Property(e => e.QuestionVersionId).HasColumnName("question_version_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Difficulty)
                .HasMaxLength(40)
                .HasColumnName("difficulty");
            entity.Property(e => e.PromptText).HasColumnName("prompt_text");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.RevisionNumber).HasColumnName("revision_number");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.QuestionVersions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_VERSION_created_by_user_id");

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionVersions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUESTION_VERSION_question_id");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("REVIEW");

            entity.HasIndex(e => e.DelegationId, "IX_REVIEW_delegation_id");

            entity.HasIndex(e => e.ReviewedVersionId, "IX_REVIEW_reviewed_version_id");

            entity.HasIndex(e => e.ReviewerUserId, "IX_REVIEW_reviewer_user_id");

            entity.HasIndex(e => e.ReviewerUserRoleId, "IX_REVIEW_reviewer_user_role_id");

            entity.HasIndex(e => e.SubmissionId, "IX_REVIEW_submission_id");

            entity.HasIndex(e => new { e.SubmissionId, e.ReviewStage }, "UQ_Review_SubmissionStage").IsUnique();

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.Decision)
                .HasMaxLength(40)
                .HasColumnName("decision");
            entity.Property(e => e.DelegationId).HasColumnName("delegation_id");
            entity.Property(e => e.Feedback).HasColumnName("feedback");
            entity.Property(e => e.ReviewStage)
                .HasMaxLength(40)
                .HasColumnName("review_stage");
            entity.Property(e => e.ReviewedAt)
                .HasPrecision(0)
                .HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedVersionId).HasColumnName("reviewed_version_id");
            entity.Property(e => e.ReviewerUserId).HasColumnName("reviewer_user_id");
            entity.Property(e => e.ReviewerUserRoleId).HasColumnName("reviewer_user_role_id");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");

            entity.HasOne(d => d.Delegation).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.DelegationId)
                .HasConstraintName("FK_REVIEW_delegation_id");

            entity.HasOne(d => d.ReviewedVersion).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ReviewedVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REVIEW_reviewed_version_id");

            entity.HasOne(d => d.ReviewerUser).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ReviewerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REVIEW_reviewer_user_id");

            entity.HasOne(d => d.ReviewerUserRole).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ReviewerUserRoleId)
                .HasConstraintName("FK_REVIEW_reviewer_user_role_id");

            entity.HasOne(d => d.Submission).WithMany(p => p.ReviewSubmissions)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REVIEW_submission_id");

            entity.HasOne(d => d.PaperSubmission).WithMany(p => p.ReviewPaperSubmissions)
                .HasPrincipalKey(p => new { p.SubmissionId, p.PaperVersionId })
                .HasForeignKey(d => new { d.SubmissionId, d.ReviewedVersionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_SubmissionVersion");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("ROLE");

            entity.HasIndex(e => e.RoleCode, "UQ_ROLE_1").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleCode)
                .HasMaxLength(40)
                .HasColumnName("role_code");
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.ToTable("SECTION");

            entity.HasIndex(e => e.ProgramId, "IX_SECTION_program_id");

            entity.HasIndex(e => e.SessionId, "IX_SECTION_session_id");

            entity.HasIndex(e => new { e.ProgramId, e.SessionId, e.SemesterNumber, e.SectionName }, "UQ_SECTION_1").IsUnique();

            entity.Property(e => e.SectionId).HasColumnName("section_id");
            entity.Property(e => e.ProgramId).HasColumnName("program_id");
            entity.Property(e => e.SectionName)
                .HasMaxLength(200)
                .HasColumnName("section_name");
            entity.Property(e => e.SemesterNumber).HasColumnName("semester_number");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Program).WithMany(p => p.Sections)
                .HasForeignKey(d => d.ProgramId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SECTION_program_id");

            entity.HasOne(d => d.Session).WithMany(p => p.Sections)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SECTION_session_id");
        });

        modelBuilder.Entity<SimilarityRun>(entity =>
        {
            entity.ToTable("SIMILARITY_RUN");

            entity.HasIndex(e => e.PaperVersionId, "IX_SIMILARITY_RUN_paper_version_id");

            entity.Property(e => e.SimilarityRunId).HasColumnName("similarity_run_id");
            entity.Property(e => e.AggregationMethod)
                .HasMaxLength(200)
                .HasColumnName("aggregation_method");
            entity.Property(e => e.AlgorithmVersion)
                .HasMaxLength(200)
                .HasColumnName("algorithm_version");
            entity.Property(e => e.ComputedAt)
                .HasPrecision(0)
                .HasColumnName("computed_at");
            entity.Property(e => e.CorpusHash)
                .HasMaxLength(256)
                .HasColumnName("corpus_hash");
            entity.Property(e => e.CorpusScope).HasColumnName("corpus_scope");
            entity.Property(e => e.MatchMode)
                .HasMaxLength(20)
                .HasDefaultValue("EXACT", "DF_SimilarityRun_Mode")
                .HasColumnName("match_mode");
            entity.Property(e => e.OverallPercent)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("overall_percent");
            entity.Property(e => e.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(e => e.RunStatus)
                .HasMaxLength(40)
                .HasColumnName("run_status");

            entity.HasOne(d => d.PaperVersion).WithMany(p => p.SimilarityRuns)
                .HasForeignKey(d => d.PaperVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SIMILARITY_RUN_paper_version_id");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingsId);

            entity.ToTable("SYSTEM_SETTINGS");

            entity.Property(e => e.SettingsId)
                .ValueGeneratedNever()
                .HasColumnName("settings_id");
            entity.Property(e => e.ActiveDatesheetId).HasColumnName("active_datesheet_id");
            entity.Property(e => e.ActiveExamCycleId).HasColumnName("active_exam_cycle_id");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.ActiveExamCycle).WithMany(p => p.SystemSettings)
                .HasForeignKey(d => d.ActiveExamCycleId)
                .HasConstraintName("FK_SYSTEM_SETTINGS_ExamCycle");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.SystemSettings)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK_SYSTEM_SETTINGS_UpdatedBy");

            entity.HasOne(d => d.Datesheet).WithMany(p => p.SystemSettings)
                .HasPrincipalKey(p => new { p.DatesheetId, p.ExamCycleId })
                .HasForeignKey(d => new { d.ActiveDatesheetId, d.ActiveExamCycleId })
                .HasConstraintName("FK_SYSTEM_SETTINGS_DatesheetCycle");
        });

        modelBuilder.Entity<TeachingAssignment>(entity =>
        {
            entity.ToTable("TEACHING_ASSIGNMENT");

            entity.HasIndex(e => e.OfferingSectionId, "IX_TEACHING_ASSIGNMENT_offering_section_id");

            entity.HasIndex(e => e.TeacherUserId, "IX_TEACHING_ASSIGNMENT_teacher_user_id");

            entity.HasIndex(e => new { e.OfferingSectionId, e.TeacherUserId }, "UX_TEACHING_ASSIGNMENT_Current")
                .IsUnique()
                .HasFilter("([ends_at] IS NULL)");

            entity.Property(e => e.TeachingAssignmentId).HasColumnName("teaching_assignment_id");
            entity.Property(e => e.EndsAt)
                .HasPrecision(0)
                .HasColumnName("ends_at");
            entity.Property(e => e.OfferingSectionId).HasColumnName("offering_section_id");
            entity.Property(e => e.StartsAt)
                .HasPrecision(0)
                .HasColumnName("starts_at");
            entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id");

            entity.HasOne(d => d.OfferingSection).WithMany(p => p.TeachingAssignments)
                .HasForeignKey(d => d.OfferingSectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TEACHING_ASSIGNMENT_offering_section_id");

            entity.HasOne(d => d.TeacherUser).WithMany(p => p.TeachingAssignments)
                .HasForeignKey(d => d.TeacherUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TEACHING_ASSIGNMENT_teacher_user_id");
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("TOPIC");

            entity.HasIndex(e => e.OutlineId, "IX_TOPIC_outline_id");

            entity.HasIndex(e => new { e.ParentTopicId, e.OutlineId }, "IX_Topic_Parent");

            entity.HasIndex(e => new { e.OutlineId, e.ParentTopicId, e.TopicTitle }, "UQ_TOPIC_1").IsUnique();

            entity.HasIndex(e => new { e.TopicId, e.OutlineId }, "UQ_TOPIC_Outline").IsUnique();

            entity.Property(e => e.TopicId).HasColumnName("topic_id");
            entity.Property(e => e.OutlineId).HasColumnName("outline_id");
            entity.Property(e => e.ParentTopicId).HasColumnName("parent_topic_id");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(1, "DF_TOPIC_sort_order")
                .HasColumnName("sort_order");
            entity.Property(e => e.TopicTitle)
                .HasMaxLength(200)
                .HasColumnName("topic_title");
            entity.Property(e => e.WeekNumber).HasColumnName("week_number");

            entity.HasOne(d => d.Outline).WithMany(p => p.Topics)
                .HasForeignKey(d => d.OutlineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOPIC_outline_id");

            entity.HasOne(d => d.TopicNavigation).WithMany(p => p.InverseTopicNavigation)
                .HasPrincipalKey(p => new { p.TopicId, p.OutlineId })
                .HasForeignKey(d => new { d.ParentTopicId, d.OutlineId })
                .HasConstraintName("FK_Topic_ParentOutline");

            entity.HasMany(d => d.Clos).WithMany(p => p.Topics)
                .UsingEntity<Dictionary<string, object>>(
                    "TopicClo",
                    r => r.HasOne<Clo>().WithMany()
                        .HasForeignKey("CloId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_TOPIC_CLO_clo_id"),
                    l => l.HasOne<Topic>().WithMany()
                        .HasForeignKey("TopicId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_TOPIC_CLO_topic_id"),
                    j =>
                    {
                        j.HasKey("TopicId", "CloId");
                        j.ToTable("TOPIC_CLO");
                        j.IndexerProperty<int>("TopicId").HasColumnName("topic_id");
                        j.IndexerProperty<int>("CloId").HasColumnName("clo_id");
                    });
        });

        modelBuilder.Entity<TopicCoverage>(entity =>
        {
            entity.HasKey(e => e.CoverageId);

            entity.ToTable("TOPIC_COVERAGE");

            entity.HasIndex(e => e.CourseExamId, "IX_TOPIC_COVERAGE_course_exam_id");

            entity.HasIndex(e => e.TeachingAssignmentId, "IX_TOPIC_COVERAGE_teaching_assignment_id");

            entity.HasIndex(e => e.TopicId, "IX_TOPIC_COVERAGE_topic_id");

            entity.HasIndex(e => new { e.TeachingAssignmentId, e.CourseExamId, e.TopicId }, "UQ_TOPIC_COVERAGE_1").IsUnique();

            entity.Property(e => e.CoverageId).HasColumnName("coverage_id");
            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.CoverageStatus)
                .HasMaxLength(40)
                .HasColumnName("coverage_status");
            entity.Property(e => e.TeachingAssignmentId).HasColumnName("teaching_assignment_id");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");

            entity.HasOne(d => d.CourseExam).WithMany(p => p.TopicCoverages)
                .HasForeignKey(d => d.CourseExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOPIC_COVERAGE_course_exam_id");

            entity.HasOne(d => d.TeachingAssignment).WithMany(p => p.TopicCoverages)
                .HasForeignKey(d => d.TeachingAssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOPIC_COVERAGE_teaching_assignment_id");

            entity.HasOne(d => d.Topic).WithMany(p => p.TopicCoverages)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOPIC_COVERAGE_topic_id");

            entity.HasOne(d => d.TopicCoverageSubmission).WithMany(p => p.TopicCoverages)
                .HasPrincipalKey(p => new { p.TeachingAssignmentId, p.CourseExamId })
                .HasForeignKey(d => new { d.TeachingAssignmentId, d.CourseExamId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TopicCoverage_Submission");
        });

        modelBuilder.Entity<TopicCoverageSubmission>(entity =>
        {
            entity.HasKey(e => e.CoverageSubmissionId);

            entity.ToTable("TOPIC_COVERAGE_SUBMISSION");

            entity.HasIndex(e => new { e.CourseExamId, e.SubmissionStatus }, "IX_CoverageSubmission_Exam");

            entity.HasIndex(e => new { e.TeachingAssignmentId, e.CourseExamId }, "UQ_CoverageSubmission_AssignmentExam").IsUnique();

            entity.Property(e => e.CoverageSubmissionId).HasColumnName("coverage_submission_id");
            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.SubmissionStatus)
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT", "DF_CoverageSubmission_Status")
                .HasColumnName("submission_status");
            entity.Property(e => e.SubmittedAt)
                .HasPrecision(0)
                .HasColumnName("submitted_at");
            entity.Property(e => e.SubmittedByUserId).HasColumnName("submitted_by_user_id");
            entity.Property(e => e.TeachingAssignmentId).HasColumnName("teaching_assignment_id");

            entity.HasOne(d => d.CourseExam).WithMany(p => p.TopicCoverageSubmissions)
                .HasForeignKey(d => d.CourseExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoverageSubmission_Exam");

            entity.HasOne(d => d.SubmittedByUser).WithMany(p => p.TopicCoverageSubmissions)
                .HasForeignKey(d => d.SubmittedByUserId)
                .HasConstraintName("FK_CoverageSubmission_Submitter");

            entity.HasOne(d => d.TeachingAssignment).WithMany(p => p.TopicCoverageSubmissions)
                .HasForeignKey(d => d.TeachingAssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoverageSubmission_Assignment");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("USER_ROLE");

            entity.HasIndex(e => e.DepartmentId, "IX_USER_ROLE_department_id");

            entity.HasIndex(e => e.RoleId, "IX_USER_ROLE_role_id");

            entity.HasIndex(e => e.UserId, "IX_USER_ROLE_user_id");

            entity.HasIndex(e => new { e.UserId, e.RoleId, e.DepartmentId, e.ValidFrom }, "UQ_USER_ROLE_1").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.RoleId, e.DepartmentId }, "UX_USER_ROLE_Open")
                .IsUnique()
                .HasFilter("([valid_until] IS NULL)");

            entity.Property(e => e.UserRoleId).HasColumnName("user_role_id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ValidFrom)
                .HasPrecision(0)
                .HasColumnName("valid_from");
            entity.Property(e => e.ValidUntil)
                .HasPrecision(0)
                .HasColumnName("valid_until");

            entity.HasOne(d => d.Department).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_USER_ROLE_department_id");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_USER_ROLE_role_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_USER_ROLE_user_id");
        });

        modelBuilder.Entity<VwActiveUserRole>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ActiveUserRole");

            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.RoleCode)
                .HasMaxLength(40)
                .HasColumnName("role_code");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserRoleId).HasColumnName("user_role_id");
        });

        modelBuilder.Entity<VwComputedCommonTopic>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ComputedCommonTopic");

            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");
            entity.Property(e => e.TopicTitle)
                .HasMaxLength(200)
                .HasColumnName("topic_title");
        });

        modelBuilder.Entity<VwCourseExam>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CourseExam");

            entity.Property(e => e.CourseCode)
                .HasMaxLength(40)
                .HasColumnName("course_code");
            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(200)
                .HasColumnName("course_title");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.ExamCycleId).HasColumnName("exam_cycle_id");
            entity.Property(e => e.ExamType)
                .HasMaxLength(40)
                .HasColumnName("exam_type");
            entity.Property(e => e.LeadDepartmentId).HasColumnName("lead_department_id");
            entity.Property(e => e.OfferingId).HasColumnName("offering_id");
            entity.Property(e => e.OutlineId).HasColumnName("outline_id");
            entity.Property(e => e.RequiredMarks)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("required_marks");
            entity.Property(e => e.ScopeLockedAt)
                .HasPrecision(0)
                .HasColumnName("scope_locked_at");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.SessionName)
                .HasMaxLength(200)
                .HasColumnName("session_name");
        });

        modelBuilder.Entity<VwCurrentExam>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CurrentExam");

            entity.Property(e => e.ActiveDatesheetId).HasColumnName("active_datesheet_id");
            entity.Property(e => e.ActiveExamCycleId).HasColumnName("active_exam_cycle_id");
            entity.Property(e => e.DatesheetStatus)
                .HasMaxLength(20)
                .HasColumnName("datesheet_status");
            entity.Property(e => e.ExamType)
                .HasMaxLength(40)
                .HasColumnName("exam_type");
            entity.Property(e => e.RevisionNumber).HasColumnName("revision_number");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.SessionName)
                .HasMaxLength(200)
                .HasColumnName("session_name");
            entity.Property(e => e.SettingsId).HasColumnName("settings_id");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<VwDatesheetHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DatesheetHistory");

            entity.Property(e => e.DatesheetId).HasColumnName("datesheet_id");
            entity.Property(e => e.ExamCycleId).HasColumnName("exam_cycle_id");
            entity.Property(e => e.ExamType)
                .HasMaxLength(40)
                .HasColumnName("exam_type");
            entity.Property(e => e.IsCurrent).HasColumnName("is_current");
            entity.Property(e => e.PublishedAt)
                .HasPrecision(0)
                .HasColumnName("published_at");
            entity.Property(e => e.PublishedByUserId).HasColumnName("published_by_user_id");
            entity.Property(e => e.RevisionNumber).HasColumnName("revision_number");
            entity.Property(e => e.SessionName)
                .HasMaxLength(200)
                .HasColumnName("session_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
        });

        modelBuilder.Entity<VwLatestClocheck>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_LatestCLOCheck");

            entity.Property(e => e.CheckId)
                .ValueGeneratedOnAdd()
                .HasColumnName("check_id");
            entity.Property(e => e.CheckStatus)
                .HasMaxLength(30)
                .HasColumnName("check_status");
            entity.Property(e => e.CheckedAt)
                .HasPrecision(0)
                .HasColumnName("checked_at");
            entity.Property(e => e.CheckerVersion)
                .HasMaxLength(100)
                .HasColumnName("checker_version");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.MappingId).HasColumnName("mapping_id");
        });

        modelBuilder.Entity<VwPaperList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PaperList");

            entity.Property(e => e.ArchivedAt)
                .HasPrecision(0)
                .HasColumnName("archived_at");
            entity.Property(e => e.CourseCode)
                .HasMaxLength(40)
                .HasColumnName("course_code");
            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(200)
                .HasColumnName("course_title");
            entity.Property(e => e.DeletedAt)
                .HasPrecision(0)
                .HasColumnName("deleted_at");
            entity.Property(e => e.DueAt)
                .HasPrecision(0)
                .HasColumnName("due_at");
            entity.Property(e => e.ExamCycleId).HasColumnName("exam_cycle_id");
            entity.Property(e => e.ExamType)
                .HasMaxLength(40)
                .HasColumnName("exam_type");
            entity.Property(e => e.FrozenAt)
                .HasPrecision(0)
                .HasColumnName("frozen_at");
            entity.Property(e => e.LeadDepartmentId).HasColumnName("lead_department_id");
            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(e => e.RevisionNumber).HasColumnName("revision_number");
            entity.Property(e => e.SessionName)
                .HasMaxLength(200)
                .HasColumnName("session_name");
            entity.Property(e => e.SetterName)
                .HasMaxLength(200)
                .HasColumnName("setter_name");
            entity.Property(e => e.SetterUserId).HasColumnName("setter_user_id");
            entity.Property(e => e.WorkflowStatus)
                .HasMaxLength(40)
                .HasColumnName("workflow_status");
        });

        modelBuilder.Entity<VwPaperVersionSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PaperVersionSummary");

            entity.Property(e => e.AddedMarks)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("added_marks");
            entity.Property(e => e.DurationMinutesSnapshot).HasColumnName("duration_minutes_snapshot");
            entity.Property(e => e.FrozenAt)
                .HasPrecision(0)
                .HasColumnName("frozen_at");
            entity.Property(e => e.MainQuestionCount).HasColumnName("main_question_count");
            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.PaperVersionId)
                .ValueGeneratedOnAdd()
                .HasColumnName("paper_version_id");
            entity.Property(e => e.RequiredMarksSnapshot)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("required_marks_snapshot");
            entity.Property(e => e.RevisionNumber).HasColumnName("revision_number");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
        });

        modelBuilder.Entity<VwRequiredCoverage>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RequiredCoverage");

            entity.Property(e => e.CourseExamId).HasColumnName("course_exam_id");
            entity.Property(e => e.CoverageSubmissionId).HasColumnName("coverage_submission_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.SectionId).HasColumnName("section_id");
            entity.Property(e => e.SectionName)
                .HasMaxLength(200)
                .HasColumnName("section_name");
            entity.Property(e => e.SubmissionStatus)
                .HasMaxLength(20)
                .HasColumnName("submission_status");
            entity.Property(e => e.SubmittedAt)
                .HasPrecision(0)
                .HasColumnName("submitted_at");
            entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id");
            entity.Property(e => e.TeachingAssignmentId).HasColumnName("teaching_assignment_id");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.ToTable("WISHLIST");

            entity.HasIndex(e => e.CourseId, "IX_WISHLIST_course_id");

            entity.HasIndex(e => e.DeletedByUserId, "IX_WISHLIST_deleted_by_user_id");

            entity.HasIndex(e => e.OwnerUserId, "IX_WISHLIST_owner_user_id");

            entity.HasIndex(e => e.PaperId, "IX_WISHLIST_paper_id");

            entity.Property(e => e.WishlistId).HasColumnName("wishlist_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.DeletedAt)
                .HasPrecision(0)
                .HasColumnName("deleted_at");
            entity.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id");
            entity.Property(e => e.DueAt)
                .HasPrecision(0)
                .HasColumnName("due_at");
            entity.Property(e => e.IsDone).HasColumnName("is_done");
            entity.Property(e => e.NoteText).HasColumnName("note_text");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.PaperId).HasColumnName("paper_id");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");

            entity.HasOne(d => d.Course).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK_WISHLIST_course_id");

            entity.HasOne(d => d.DeletedByUser).WithMany(p => p.WishlistDeletedByUsers)
                .HasForeignKey(d => d.DeletedByUserId)
                .HasConstraintName("FK_WISHLIST_deleted_by_user_id");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.WishlistOwnerUsers)
                .HasForeignKey(d => d.OwnerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WISHLIST_owner_user_id");

            entity.HasOne(d => d.Paper).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.PaperId)
                .HasConstraintName("FK_WISHLIST_paper_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

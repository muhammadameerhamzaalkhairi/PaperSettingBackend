using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class AppUser
{
    public int UserId { get; set; }

    public string StaffNumber { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int? HomeDepartmentId { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Designation { get; set; }

    public int? ProfilePhotoFileAssetId { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    public virtual ICollection<Comment> CommentAcknowledgedByUsers { get; set; } = new List<Comment>();

    public virtual ICollection<Comment> CommentAuthorUsers { get; set; } = new List<Comment>();

    public virtual ICollection<CourseExam> CourseExams { get; set; } = new List<CourseExam>();

    public virtual ICollection<Datesheet> Datesheets { get; set; } = new List<Datesheet>();

    public virtual ICollection<DirectorDelegation> DirectorDelegationDelegateUsers { get; set; } = new List<DirectorDelegation>();

    public virtual ICollection<DirectorDelegation> DirectorDelegationRevokedByUsers { get; set; } = new List<DirectorDelegation>();

    public virtual ICollection<FileAsset> FileAssets { get; set; } = new List<FileAsset>();

    public virtual Department? HomeDepartment { get; set; }

    public virtual ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PaperAssignment> PaperAssignments { get; set; } = new List<PaperAssignment>();

    public virtual ICollection<PaperParticipant> PaperParticipants { get; set; } = new List<PaperParticipant>();

    public virtual ICollection<PaperSubmission> PaperSubmissions { get; set; } = new List<PaperSubmission>();

    public virtual ICollection<PaperVersion> PaperVersions { get; set; } = new List<PaperVersion>();

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();

    public virtual ICollection<PhysicalReceipt> PhysicalReceiptDeliveredByUsers { get; set; } = new List<PhysicalReceipt>();

    public virtual ICollection<PhysicalReceipt> PhysicalReceiptReceivedByUsers { get; set; } = new List<PhysicalReceipt>();

    public virtual ICollection<PrintExport> PrintExports { get; set; } = new List<PrintExport>();

    public virtual FileAsset? ProfilePhotoFileAsset { get; set; }

    public virtual ICollection<Question> QuestionAuthorUsers { get; set; } = new List<Question>();

    public virtual ICollection<Question> QuestionDeletedByUsers { get; set; } = new List<Question>();

    public virtual ICollection<QuestionVersion> QuestionVersions { get; set; } = new List<QuestionVersion>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();

    public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();

    public virtual ICollection<TopicCoverageSubmission> TopicCoverageSubmissions { get; set; } = new List<TopicCoverageSubmission>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<Wishlist> WishlistDeletedByUsers { get; set; } = new List<Wishlist>();

    public virtual ICollection<Wishlist> WishlistOwnerUsers { get; set; } = new List<Wishlist>();
}

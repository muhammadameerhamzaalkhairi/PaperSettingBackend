using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int SubmissionId { get; set; }

    public int ReviewedVersionId { get; set; }

    public int ReviewerUserId { get; set; }

    public int? ReviewerUserRoleId { get; set; }

    public int? DelegationId { get; set; }

    public string ReviewStage { get; set; } = null!;

    public string Decision { get; set; } = null!;

    public string? Feedback { get; set; }

    public DateTime ReviewedAt { get; set; }

    public virtual DirectorDelegation? Delegation { get; set; }

    public virtual PaperSubmission PaperSubmission { get; set; } = null!;

    public virtual PhysicalReceipt? PhysicalReceipt { get; set; }

    public virtual ICollection<PrintExport> PrintExports { get; set; } = new List<PrintExport>();

    public virtual PaperVersion ReviewedVersion { get; set; } = null!;

    public virtual AppUser ReviewerUser { get; set; } = null!;

    public virtual UserRole? ReviewerUserRole { get; set; }

    public virtual PaperSubmission Submission { get; set; } = null!;
}

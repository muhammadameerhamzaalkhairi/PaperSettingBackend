using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PaperVersion
{
    public int PaperVersionId { get; set; }

    public int PaperId { get; set; }

    public int RevisionNumber { get; set; }

    public int? BasedOnVersionId { get; set; }

    public int? CopiedFromVersionId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string RevisionReason { get; set; } = null!;

    public string? Instructions { get; set; }

    public string ContentHash { get; set; } = null!;

    public decimal RequiredMarksSnapshot { get; set; }

    public int DurationMinutesSnapshot { get; set; }

    public DateTime? FrozenAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual PaperVersion? BasedOnVersion { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual PaperVersion? CopiedFromVersion { get; set; }

    public virtual AppUser CreatedByUser { get; set; } = null!;

    public virtual ICollection<PaperVersion> InverseBasedOnVersion { get; set; } = new List<PaperVersion>();

    public virtual ICollection<PaperVersion> InverseCopiedFromVersion { get; set; } = new List<PaperVersion>();

    public virtual ICollection<PaperVersion> InversePaperVersionNavigation { get; set; } = new List<PaperVersion>();

    public virtual Paper Paper { get; set; } = null!;

    public virtual ICollection<PaperItem> PaperItems { get; set; } = new List<PaperItem>();

    public virtual ICollection<PaperMatch> PaperMatches { get; set; } = new List<PaperMatch>();

    public virtual PaperSubmission? PaperSubmission { get; set; }

    public virtual ICollection<PaperTopicCheck> PaperTopicChecks { get; set; } = new List<PaperTopicCheck>();

    public virtual ICollection<PaperVersionCommonTopic> PaperVersionCommonTopics { get; set; } = new List<PaperVersionCommonTopic>();

    public virtual PaperVersion? PaperVersionNavigation { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SimilarityRun> SimilarityRuns { get; set; } = new List<SimilarityRun>();
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class QuestionVersion
{
    public int QuestionVersionId { get; set; }

    public int QuestionId { get; set; }

    public int RevisionNumber { get; set; }

    public string PromptText { get; set; } = null!;

    public string Difficulty { get; set; } = null!;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CloMapping> CloMappings { get; set; } = new List<CloMapping>();

    public virtual AppUser CreatedByUser { get; set; } = null!;

    public virtual ICollection<PaperItem> PaperItems { get; set; } = new List<PaperItem>();

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<QuestionAttachment> QuestionAttachments { get; set; } = new List<QuestionAttachment>();

    public virtual ICollection<QuestionPart> QuestionParts { get; set; } = new List<QuestionPart>();
}

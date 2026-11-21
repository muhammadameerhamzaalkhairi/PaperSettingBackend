using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class QuestionPart
{
    public int QuestionPartId { get; set; }

    public int QuestionVersionId { get; set; }

    public int? ParentPartId { get; set; }

    public string PartKind { get; set; } = null!;

    public string? Difficulty { get; set; }

    public string PartLabel { get; set; } = null!;

    public int PartOrder { get; set; }

    public string PartText { get; set; } = null!;

    public int? TopicId { get; set; }

    public decimal? Marks { get; set; }

    public virtual ICollection<CloMapping> CloMappings { get; set; } = new List<CloMapping>();

    public virtual ICollection<QuestionPart> InverseQuestionPartNavigation { get; set; } = new List<QuestionPart>();

    public virtual ICollection<PaperTopicCheck> PaperTopicChecks { get; set; } = new List<PaperTopicCheck>();

    public virtual ICollection<QuestionAttachment> QuestionAttachmentQuestionPartNavigations { get; set; } = new List<QuestionAttachment>();

    public virtual ICollection<QuestionAttachment> QuestionAttachmentQuestionParts { get; set; } = new List<QuestionAttachment>();

    public virtual QuestionPart? QuestionPartNavigation { get; set; }

    public virtual QuestionVersion QuestionVersion { get; set; } = null!;

    public virtual Topic? Topic { get; set; }
}

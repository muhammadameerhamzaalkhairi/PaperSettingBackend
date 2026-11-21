using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PaperItem
{
    public int PaperItemId { get; set; }

    public int PaperVersionId { get; set; }

    public int QuestionVersionId { get; set; }

    public string Placement { get; set; } = null!;

    public int SortOrder { get; set; }

    public string? DisplayNumber { get; set; }

    public virtual ICollection<Comment> CommentPaperItemNavigations { get; set; } = new List<Comment>();

    public virtual ICollection<Comment> CommentPaperItems { get; set; } = new List<Comment>();

    public virtual PaperVersion PaperVersion { get; set; } = null!;

    public virtual ICollection<QuestionMatch> QuestionMatchCurrentItems { get; set; } = new List<QuestionMatch>();

    public virtual ICollection<QuestionMatch> QuestionMatchSourceItems { get; set; } = new List<QuestionMatch>();

    public virtual QuestionVersion QuestionVersion { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}

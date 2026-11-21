using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int PaperId { get; set; }

    public int AuthorUserId { get; set; }

    public int? SourcePaperItemId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public virtual AppUser AuthorUser { get; set; } = null!;

    public virtual AppUser? DeletedByUser { get; set; }

    public virtual Paper Paper { get; set; } = null!;

    public virtual ICollection<QuestionVersion> QuestionVersions { get; set; } = new List<QuestionVersion>();

    public virtual PaperItem? SourcePaperItem { get; set; }
}

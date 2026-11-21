using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int PaperVersionId { get; set; }

    public int? PaperItemId { get; set; }

    public int? ParentCommentId { get; set; }

    public int AuthorUserId { get; set; }

    public string TargetType { get; set; } = null!;

    public string CommentText { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int? AcknowledgedByUserId { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public virtual AppUser? AcknowledgedByUser { get; set; }

    public virtual AppUser AuthorUser { get; set; } = null!;

    public virtual ICollection<Comment> InverseParentComment { get; set; } = new List<Comment>();

    public virtual PaperItem? PaperItem { get; set; }

    public virtual PaperItem? PaperItemNavigation { get; set; }

    public virtual PaperVersion PaperVersion { get; set; } = null!;

    public virtual Comment? ParentComment { get; set; }
}

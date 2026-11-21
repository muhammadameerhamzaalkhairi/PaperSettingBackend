using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PaperSubmission
{
    public int SubmissionId { get; set; }

    public int PaperVersionId { get; set; }

    public int SubmissionNumber { get; set; }

    public int SubmittedByUserId { get; set; }

    public DateTime SubmittedAt { get; set; }

    public virtual PaperVersion PaperVersion { get; set; } = null!;

    public virtual ICollection<Review> ReviewPaperSubmissions { get; set; } = new List<Review>();

    public virtual ICollection<Review> ReviewSubmissions { get; set; } = new List<Review>();

    public virtual AppUser SubmittedByUser { get; set; } = null!;
}

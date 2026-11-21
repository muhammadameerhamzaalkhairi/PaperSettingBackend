using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class VwPaperVersionSummary
{
    public int PaperVersionId { get; set; }

    public int PaperId { get; set; }

    public int RevisionNumber { get; set; }

    public DateTime? FrozenAt { get; set; }

    public decimal RequiredMarksSnapshot { get; set; }

    public int DurationMinutesSnapshot { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public int? MainQuestionCount { get; set; }

    public decimal? AddedMarks { get; set; }
}

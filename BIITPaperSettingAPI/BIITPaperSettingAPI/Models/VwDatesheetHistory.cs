using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class VwDatesheetHistory
{
    public int DatesheetId { get; set; }

    public int ExamCycleId { get; set; }

    public string SessionName { get; set; } = null!;

    public string ExamType { get; set; } = null!;

    public int RevisionNumber { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PublishedAt { get; set; }

    public int? PublishedByUserId { get; set; }

    public bool? IsCurrent { get; set; }
}

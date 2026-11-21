using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class VwPaperList
{
    public int PaperId { get; set; }

    public int CourseExamId { get; set; }

    public string WorkflowStatus { get; set; } = null!;

    public DateTime? ArchivedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string CourseCode { get; set; } = null!;

    public string CourseTitle { get; set; } = null!;

    public string SessionName { get; set; } = null!;

    public string ExamType { get; set; } = null!;

    public int ExamCycleId { get; set; }

    public int LeadDepartmentId { get; set; }

    public int? SetterUserId { get; set; }

    public string? SetterName { get; set; }

    public DateTime? DueAt { get; set; }

    public int? PaperVersionId { get; set; }

    public int? RevisionNumber { get; set; }

    public DateTime? FrozenAt { get; set; }
}

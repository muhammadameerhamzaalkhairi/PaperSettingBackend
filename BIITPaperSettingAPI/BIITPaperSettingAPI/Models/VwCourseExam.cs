using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class VwCourseExam
{
    public int CourseExamId { get; set; }

    public int OfferingId { get; set; }

    public int ExamCycleId { get; set; }

    public decimal RequiredMarks { get; set; }

    public int DurationMinutes { get; set; }

    public DateTime? ScopeLockedAt { get; set; }

    public int CourseId { get; set; }

    public int OutlineId { get; set; }

    public int LeadDepartmentId { get; set; }

    public int SessionId { get; set; }

    public string CourseCode { get; set; } = null!;

    public string CourseTitle { get; set; } = null!;

    public string SessionName { get; set; } = null!;

    public string ExamType { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class VwRequiredCoverage
{
    public int CourseExamId { get; set; }

    public int TeachingAssignmentId { get; set; }

    public int TeacherUserId { get; set; }

    public int SectionId { get; set; }

    public string SectionName { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public int? CoverageSubmissionId { get; set; }

    public string? SubmissionStatus { get; set; }

    public DateTime? SubmittedAt { get; set; }
}

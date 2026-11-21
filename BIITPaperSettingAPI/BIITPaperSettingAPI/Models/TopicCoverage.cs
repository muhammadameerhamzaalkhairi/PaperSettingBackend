using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class TopicCoverage
{
    public int CoverageId { get; set; }

    public int TeachingAssignmentId { get; set; }

    public int CourseExamId { get; set; }

    public int TopicId { get; set; }

    public string CoverageStatus { get; set; } = null!;

    public virtual CourseExam CourseExam { get; set; } = null!;

    public virtual TeachingAssignment TeachingAssignment { get; set; } = null!;

    public virtual Topic Topic { get; set; } = null!;

    public virtual TopicCoverageSubmission TopicCoverageSubmission { get; set; } = null!;
}

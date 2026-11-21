using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class CourseExam
{
    public int CourseExamId { get; set; }

    public int OfferingId { get; set; }

    public int ExamCycleId { get; set; }

    public decimal RequiredMarks { get; set; }

    public int DurationMinutes { get; set; }

    public int ConfiguredByUserId { get; set; }

    public DateTime? ScopeLockedAt { get; set; }

    public virtual AppUser ConfiguredByUser { get; set; } = null!;

    public virtual ICollection<ExamClo> ExamClos { get; set; } = new List<ExamClo>();

    public virtual ICollection<ExamCommonTopic> ExamCommonTopics { get; set; } = new List<ExamCommonTopic>();

    public virtual ExamCycle ExamCycle { get; set; } = null!;

    public virtual ICollection<ExamSchedule> ExamScheduleCourseExamNavigations { get; set; } = new List<ExamSchedule>();

    public virtual ICollection<ExamSchedule> ExamScheduleCourseExams { get; set; } = new List<ExamSchedule>();

    public virtual CourseOffering Offering { get; set; } = null!;

    public virtual Paper? Paper { get; set; }

    public virtual ICollection<TopicCoverageSubmission> TopicCoverageSubmissions { get; set; } = new List<TopicCoverageSubmission>();

    public virtual ICollection<TopicCoverage> TopicCoverages { get; set; } = new List<TopicCoverage>();
}

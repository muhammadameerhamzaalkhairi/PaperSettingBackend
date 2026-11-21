using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class TopicCoverageSubmission
{
    public int CoverageSubmissionId { get; set; }

    public int TeachingAssignmentId { get; set; }

    public int CourseExamId { get; set; }

    public string SubmissionStatus { get; set; } = null!;

    public int? SubmittedByUserId { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual CourseExam CourseExam { get; set; } = null!;

    public virtual AppUser? SubmittedByUser { get; set; }

    public virtual TeachingAssignment TeachingAssignment { get; set; } = null!;

    public virtual ICollection<TopicCoverage> TopicCoverages { get; set; } = new List<TopicCoverage>();
}

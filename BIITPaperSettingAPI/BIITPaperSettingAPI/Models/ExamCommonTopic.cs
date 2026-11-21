using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class ExamCommonTopic
{
    public int CommonTopicId { get; set; }

    public int CourseExamId { get; set; }

    public int TopicId { get; set; }

    public DateTime ComputedAt { get; set; }

    public virtual CourseExam CourseExam { get; set; } = null!;

    public virtual ICollection<PaperTopicCheck> PaperTopicChecks { get; set; } = new List<PaperTopicCheck>();

    public virtual Topic Topic { get; set; } = null!;
}

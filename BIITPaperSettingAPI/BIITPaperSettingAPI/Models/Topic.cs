using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Topic
{
    public int TopicId { get; set; }

    public int OutlineId { get; set; }

    public string TopicTitle { get; set; } = null!;

    public int? WeekNumber { get; set; }

    public int? ParentTopicId { get; set; }

    public int SortOrder { get; set; }

    public virtual ICollection<ExamCommonTopic> ExamCommonTopics { get; set; } = new List<ExamCommonTopic>();

    public virtual ICollection<Topic> InverseTopicNavigation { get; set; } = new List<Topic>();

    public virtual CourseOutline Outline { get; set; } = null!;

    public virtual ICollection<PaperVersionCommonTopic> PaperVersionCommonTopics { get; set; } = new List<PaperVersionCommonTopic>();

    public virtual ICollection<QuestionPart> QuestionParts { get; set; } = new List<QuestionPart>();

    public virtual ICollection<TopicCoverage> TopicCoverages { get; set; } = new List<TopicCoverage>();

    public virtual Topic? TopicNavigation { get; set; }

    public virtual ICollection<Clo> Clos { get; set; } = new List<Clo>();
}

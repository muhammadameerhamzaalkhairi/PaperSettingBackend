using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PaperTopicCheck
{
    public int TopicCheckId { get; set; }

    public int PaperVersionId { get; set; }

    public int QuestionPartId { get; set; }

    public int? CommonTopicId { get; set; }

    public string CheckStatus { get; set; } = null!;

    public string Explanation { get; set; } = null!;

    public DateTime CheckedAt { get; set; }

    public virtual ExamCommonTopic? CommonTopic { get; set; }

    public virtual PaperVersion PaperVersion { get; set; } = null!;

    public virtual QuestionPart QuestionPart { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class VwComputedCommonTopic
{
    public int CourseExamId { get; set; }

    public int TopicId { get; set; }

    public string TopicTitle { get; set; } = null!;
}

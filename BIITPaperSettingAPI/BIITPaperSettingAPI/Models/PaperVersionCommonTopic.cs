using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PaperVersionCommonTopic
{
    public int PaperVersionId { get; set; }

    public int TopicId { get; set; }

    public string TopicTitleSnapshot { get; set; } = null!;

    public virtual PaperVersion PaperVersion { get; set; } = null!;

    public virtual Topic Topic { get; set; } = null!;
}

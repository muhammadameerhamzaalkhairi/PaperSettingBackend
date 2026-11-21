using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Clo
{
    public int CloId { get; set; }

    public int OutlineId { get; set; }

    public string CloCode { get; set; } = null!;

    public string CloStatement { get; set; } = null!;

    public virtual ICollection<ExamClo> ExamClos { get; set; } = new List<ExamClo>();

    public virtual CourseOutline Outline { get; set; } = null!;

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}

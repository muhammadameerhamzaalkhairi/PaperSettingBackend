using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class ExamClo
{
    public int ExamCloId { get; set; }

    public int CourseExamId { get; set; }

    public int CloId { get; set; }

    public virtual Clo Clo { get; set; } = null!;

    public virtual ICollection<CloMapping> CloMappings { get; set; } = new List<CloMapping>();

    public virtual CourseExam CourseExam { get; set; } = null!;
}

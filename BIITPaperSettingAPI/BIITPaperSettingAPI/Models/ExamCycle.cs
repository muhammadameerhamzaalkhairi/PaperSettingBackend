using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class ExamCycle
{
    public int ExamCycleId { get; set; }

    public int SessionId { get; set; }

    public string ExamType { get; set; } = null!;

    public virtual ICollection<CourseExam> CourseExams { get; set; } = new List<CourseExam>();

    public virtual Datesheet? Datesheet { get; set; }

    public virtual AcademicSession Session { get; set; } = null!;

    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();
}

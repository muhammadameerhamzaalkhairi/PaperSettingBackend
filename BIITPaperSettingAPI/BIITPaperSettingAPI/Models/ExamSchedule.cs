using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class ExamSchedule
{
    public int ScheduleId { get; set; }

    public int DatesheetId { get; set; }

    public int CourseExamId { get; set; }

    public int ExamCycleId { get; set; }

    public DateOnly ExamDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual CourseExam CourseExam { get; set; } = null!;

    public virtual CourseExam CourseExamNavigation { get; set; } = null!;

    public virtual Datesheet Datesheet { get; set; } = null!;

    public virtual Datesheet DatesheetNavigation { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class AcademicSession
{
    public int SessionId { get; set; }

    public string SessionName { get; set; } = null!;

    public DateOnly StartsOn { get; set; }

    public DateOnly EndsOn { get; set; }

    public virtual ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();

    public virtual ICollection<ExamCycle> ExamCycles { get; set; } = new List<ExamCycle>();

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
}

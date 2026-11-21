using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class CourseOffering
{
    public int OfferingId { get; set; }

    public int CourseId { get; set; }

    public int OutlineId { get; set; }

    public int SessionId { get; set; }

    public int LeadDepartmentId { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<CourseExam> CourseExams { get; set; } = new List<CourseExam>();

    public virtual CourseOutline CourseOutline { get; set; } = null!;

    public virtual Department LeadDepartment { get; set; } = null!;

    public virtual ICollection<OfferingSection> OfferingSections { get; set; } = new List<OfferingSection>();

    public virtual CourseOutline Outline { get; set; } = null!;

    public virtual AcademicSession Session { get; set; } = null!;
}

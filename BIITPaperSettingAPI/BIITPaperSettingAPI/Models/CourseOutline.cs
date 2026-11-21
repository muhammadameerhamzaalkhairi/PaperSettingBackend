using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class CourseOutline
{
    public int OutlineId { get; set; }

    public int CourseId { get; set; }

    public int VersionNumber { get; set; }

    public DateTime? PublishedAt { get; set; }

    public virtual ICollection<Clo> Clos { get; set; } = new List<Clo>();

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<CourseOffering> CourseOfferingCourseOutlines { get; set; } = new List<CourseOffering>();

    public virtual ICollection<CourseOffering> CourseOfferingOutlines { get; set; } = new List<CourseOffering>();

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}

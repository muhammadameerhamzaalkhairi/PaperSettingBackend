using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class OfferingSection
{
    public int OfferingSectionId { get; set; }

    public int OfferingId { get; set; }

    public int SectionId { get; set; }

    public virtual CourseOffering Offering { get; set; } = null!;

    public virtual Section Section { get; set; } = null!;

    public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
}

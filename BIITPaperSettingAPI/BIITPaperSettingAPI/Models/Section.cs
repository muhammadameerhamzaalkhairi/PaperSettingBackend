using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Section
{
    public int SectionId { get; set; }

    public int ProgramId { get; set; }

    public int SessionId { get; set; }

    public string SectionName { get; set; } = null!;

    public int SemesterNumber { get; set; }

    public virtual ICollection<OfferingSection> OfferingSections { get; set; } = new List<OfferingSection>();

    public virtual Program Program { get; set; } = null!;

    public virtual AcademicSession Session { get; set; } = null!;
}

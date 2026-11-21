using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Program
{
    public int ProgramId { get; set; }

    public int DepartmentId { get; set; }

    public string ProgramCode { get; set; } = null!;

    public string ProgramName { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
}

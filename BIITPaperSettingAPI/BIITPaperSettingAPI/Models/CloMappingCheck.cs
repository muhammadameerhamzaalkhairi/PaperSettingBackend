using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class CloMappingCheck
{
    public int CheckId { get; set; }

    public int MappingId { get; set; }

    public string CheckStatus { get; set; } = null!;

    public string Explanation { get; set; } = null!;

    public string CheckerVersion { get; set; } = null!;

    public DateTime CheckedAt { get; set; }

    public virtual CloMapping Mapping { get; set; } = null!;
}

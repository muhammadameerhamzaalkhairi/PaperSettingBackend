using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class VwLatestClocheck
{
    public int CheckId { get; set; }

    public int MappingId { get; set; }

    public string CheckStatus { get; set; } = null!;

    public string Explanation { get; set; } = null!;

    public string CheckerVersion { get; set; } = null!;

    public DateTime CheckedAt { get; set; }
}

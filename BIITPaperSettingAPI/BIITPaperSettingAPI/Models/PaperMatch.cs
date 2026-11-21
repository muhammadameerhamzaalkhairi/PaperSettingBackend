using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PaperMatch
{
    public int PaperMatchId { get; set; }

    public int SimilarityRunId { get; set; }

    public int SourcePaperVersionId { get; set; }

    public decimal SimilarityPercent { get; set; }

    public virtual SimilarityRun SimilarityRun { get; set; } = null!;

    public virtual PaperVersion SourcePaperVersion { get; set; } = null!;
}

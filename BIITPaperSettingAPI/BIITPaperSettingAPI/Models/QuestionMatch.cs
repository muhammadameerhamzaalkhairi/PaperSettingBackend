using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class QuestionMatch
{
    public int QuestionMatchId { get; set; }

    public int SimilarityRunId { get; set; }

    public int CurrentItemId { get; set; }

    public int SourceItemId { get; set; }

    public decimal SimilarityPercent { get; set; }

    public virtual PaperItem CurrentItem { get; set; } = null!;

    public virtual SimilarityRun SimilarityRun { get; set; } = null!;

    public virtual PaperItem SourceItem { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class SimilarityRun
{
    public int SimilarityRunId { get; set; }

    public int PaperVersionId { get; set; }

    public string MatchMode { get; set; } = null!;

    public string AlgorithmVersion { get; set; } = null!;

    public string CorpusScope { get; set; } = null!;

    public string CorpusHash { get; set; } = null!;

    public string AggregationMethod { get; set; } = null!;

    public string RunStatus { get; set; } = null!;

    public decimal? OverallPercent { get; set; }

    public DateTime? ComputedAt { get; set; }

    public virtual ICollection<PaperMatch> PaperMatches { get; set; } = new List<PaperMatch>();

    public virtual PaperVersion PaperVersion { get; set; } = null!;

    public virtual ICollection<QuestionMatch> QuestionMatches { get; set; } = new List<QuestionMatch>();
}

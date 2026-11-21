using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class CloMapping
{
    public int MappingId { get; set; }

    public int QuestionVersionId { get; set; }

    public int? QuestionPartId { get; set; }

    public int ExamCloId { get; set; }

    public DateTime MappedAt { get; set; }

    public virtual ICollection<CloMappingCheck> CloMappingChecks { get; set; } = new List<CloMappingCheck>();

    public virtual ExamClo ExamClo { get; set; } = null!;

    public virtual QuestionPart? QuestionPart { get; set; }

    public virtual QuestionVersion QuestionVersion { get; set; } = null!;
}

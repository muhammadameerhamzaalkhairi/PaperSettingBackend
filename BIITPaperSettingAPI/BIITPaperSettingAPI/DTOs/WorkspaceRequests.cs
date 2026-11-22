using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class RevisionRequest
{
    [Required, MaxLength(500)] public string Reason { get; set; } = "";
}

public class DraftChange
{
    // JSON mein byte array Base64 hota hai. SQL row_version hamesha 8 bytes hai.
    [Required, MinLength(8), MaxLength(8)] public byte[] RowVersion { get; set; } = [];
}

public class PlacementRequest : DraftChange
{
    [Required, RegularExpression("^(MAIN|EXTRA)$")] public string Placement { get; set; } = "";
}

public class ReorderRequest : PlacementRequest
{
    [Required] public List<int>? ItemIds { get; set; }
}

public class InstructionsRequest : DraftChange
{
    public string? Instructions { get; set; }
}

public class QuestionRequest : DraftChange
{
    [Required] public string Prompt { get; set; } = "";
    [Required, RegularExpression("^(EASY|MEDIUM|HARD)$")] public string Difficulty { get; set; } = "";
    [Required, RegularExpression("^(MAIN|EXTRA)$")] public string Placement { get; set; } = "EXTRA";
    [Required, MinLength(1)] public List<QuestionPartRequest> Parts { get; set; } = [];
    [Required] public List<QuestionCloRequest> Mappings { get; set; } = [];
}

public class QuestionPartRequest
{
    [Range(1,int.MaxValue)] public int Key { get; set; }
    [Range(1,int.MaxValue)] public int? ParentKey { get; set; }
    [Required, RegularExpression("^(GROUP|ITEM)$")] public string Kind { get; set; } = "";
    [Required, MaxLength(200)] public string Label { get; set; } = "";
    public string Body { get; set; } = "";
    [Range(1,int.MaxValue)] public int? TopicId { get; set; }
    public decimal? Marks { get; set; }
    [RegularExpression("^(EASY|MEDIUM|HARD)$")] public string? Difficulty { get; set; }
}

public class QuestionCloRequest
{
    [Range(1,int.MaxValue)] public int? PartKey { get; set; }
    [Range(1,int.MaxValue)] public int ExamCloId { get; set; }
}

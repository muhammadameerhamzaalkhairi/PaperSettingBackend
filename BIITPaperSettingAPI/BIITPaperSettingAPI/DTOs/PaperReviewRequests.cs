using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class DirectorEditRequest : QuestionRequest
{
    [Required, MaxLength(500)] public string Reason { get; set; } = "";
    [Range(1,int.MaxValue)] public int? DelegationId { get; set; }
}

public class PaperDecisionRequest
{
    [Required, RegularExpression("^(HOD|DIRECTOR)$")] public string Stage { get; set; } = "";
    [Required, RegularExpression("^(APPROVED|CHANGES_REQUESTED)$")] public string Decision { get; set; } = "";
    public string? Reason { get; set; }
    [Range(1,int.MaxValue)] public int? DelegationId { get; set; }
}

namespace BIITPaperSettingAPI.DTOs;

public class CurrentExamResponse
{
    public int? SettingsId { get; set; }
    public int? ActiveExamCycleId { get; set; }
    public int? ActiveDatesheetId { get; set; }
    public int? SessionId { get; set; }
    public string? SessionName { get; set; }
    public string? ExamType { get; set; }
    public int? RevisionNumber { get; set; }
    public string? DatesheetStatus { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

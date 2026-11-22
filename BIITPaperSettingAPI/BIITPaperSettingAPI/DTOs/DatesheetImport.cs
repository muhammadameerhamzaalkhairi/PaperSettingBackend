namespace BIITPaperSettingAPI.DTOs;

public record ScheduleRow(int course_exam_id, DateOnly exam_date, TimeOnly start_time, TimeOnly end_time);
public record ImportIssue(int? Row, string Field, string Message);

public class DatesheetImport
{
    public List<ScheduleRow> Schedule { get; } = [];
    public List<ImportIssue> Errors { get; } = [];
}

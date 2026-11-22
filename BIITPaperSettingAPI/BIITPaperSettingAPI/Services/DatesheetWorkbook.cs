using System.Globalization;
using System.IO.Compression;
using ClosedXML.Excel;
using BIITPaperSettingAPI.DTOs;

namespace BIITPaperSettingAPI.Services;

public static class DatesheetWorkbook
{
    private static readonly string[] Headers = ["course_exam_id", "exam_date", "start_time", "end_time"];

    // =====================================================
    // STEP 1: CHECK WORKBOOK AND HEADERS
    // =====================================================
    // Excel row number saath rakhein taake Data Cell usi jagah correction kar sake.
    public static DatesheetImport Read(Stream stream, HashSet<int> allowed, CancellationToken cancellation)
    {
        var result = new DatesheetImport();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            if (archive.Entries.Count > 500 || archive.Entries.Sum(entry => entry.Length) > 16 * 1024 * 1024)
                throw new InvalidDataException("Workbook too large.");
        }
        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);
        if (workbook.Worksheets.Count != 1)
            throw new InvalidDataException("Use one schedule sheet.");
        var sheet = workbook.Worksheet(1);
        for (int column = 1; column <= Headers.Length; column++)
        {
            var header = sheet.Cell(1, column);
            if (header.HasFormula || !string.Equals(header.GetString().Trim(), Headers[column-1], StringComparison.OrdinalIgnoreCase))
                result.Errors.Add(new(1, Headers[column-1], $"Expected column {column} to be {Headers[column-1]}."));
        }
        if (result.Errors.Count > 0)
            return result;
        int last = sheet.LastRowUsed()?.RowNumber() ?? 1;
        if (last > 5001 || sheet.LastColumnUsed()?.ColumnNumber() > 4)
            throw new InvalidDataException("Use four columns and up to 5000 data rows.");

        // =====================================================
        // STEP 2: VALIDATE SCHEDULE ROWS
        // =====================================================
        var used = new HashSet<int>();
        for (int row = 2; row <= last; row++)
        {
            cancellation.ThrowIfCancellationRequested();
            var cells = sheet.Row(row);
            if (cells.IsEmpty())
                continue;
            if (cells.CellsUsed().Any(cell => cell.HasFormula))
            {
                result.Errors.Add(new(row, "row", "Enter values instead of formulas."));
                continue;
            }
            int before = result.Errors.Count;
            if (!int.TryParse(cells.Cell(1).GetString(), out int course) || !allowed.Contains(course))
                result.Errors.Add(new(row, Headers[0], "Choose a course exam in the selected cycle."));
            else if (!used.Add(course))
                result.Errors.Add(new(row, Headers[0], "This course exam occurs twice."));

            var dateCell = cells.Cell(2);
            DateOnly date;
            if (dateCell.DataType == XLDataType.DateTime)
                date = DateOnly.FromDateTime(dateCell.GetDateTime());
            else if (!DateOnly.TryParseExact(dateCell.GetString().Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                result.Errors.Add(new(row, Headers[1], "Use an Excel date or yyyy-MM-dd."));
            bool validStart = ReadTime(cells.Cell(3), out var start);
            bool validEnd = ReadTime(cells.Cell(4), out var end);
            if (!validStart) result.Errors.Add(new(row, Headers[2], "Use a time such as 09:00."));
            if (!validEnd) result.Errors.Add(new(row, Headers[3], "Use a time such as 11:00."));
            if (validStart && validEnd && end <= start)
                result.Errors.Add(new(row, Headers[3], "End time must follow start time."));
            if (result.Errors.Count == before)
                result.Schedule.Add(new(course, date, start, end));
        }
        if (result.Schedule.Count == 0 && result.Errors.Count == 0)
            result.Errors.Add(new(null, "schedule", "At least one course is required."));
        return result;
    }

    private static bool ReadTime(IXLCell cell, out TimeOnly time)
    {
        time = default;
        if (cell.DataType == XLDataType.DateTime)
        {
            time = TimeOnly.FromDateTime(cell.GetDateTime());
            return true;
        }
        if (cell.DataType == XLDataType.TimeSpan || cell.DataType == XLDataType.Number)
        {
            if (!cell.TryGetValue<TimeSpan>(out var span) || span < TimeSpan.Zero || span >= TimeSpan.FromDays(1))
                return false;
            time = TimeOnly.FromTimeSpan(span);
            return true;
        }
        return TimeOnly.TryParseExact(cell.GetString().Trim(), ["H:mm","HH:mm","H:mm:ss","HH:mm:ss"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class ErrorLog
{
    public long ErrorLogId { get; set; }

    public string ErrorSource { get; set; } = null!;

    public string ErrorMessage { get; set; } = null!;

    public string? StackTrace { get; set; }

    public string? ProcedureName { get; set; }

    public int? ErrorNumber { get; set; }

    public int? ErrorLine { get; set; }

    public string? RequestPath { get; set; }

    public string? TraceId { get; set; }

    public int? UserId { get; set; }

    public DateTime OccurredAt { get; set; }
}

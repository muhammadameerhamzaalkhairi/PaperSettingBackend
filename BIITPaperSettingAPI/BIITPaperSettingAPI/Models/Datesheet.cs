using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Datesheet
{
    public int DatesheetId { get; set; }

    public int ExamCycleId { get; set; }

    public int? ImportBatchId { get; set; }

    public int RevisionNumber { get; set; }

    public string Status { get; set; } = null!;

    public int? PublishedByUserId { get; set; }

    public DateTime? PublishedAt { get; set; }

    public virtual ExamCycle ExamCycle { get; set; } = null!;

    public virtual ICollection<ExamSchedule> ExamScheduleDatesheetNavigations { get; set; } = new List<ExamSchedule>();

    public virtual ICollection<ExamSchedule> ExamScheduleDatesheets { get; set; } = new List<ExamSchedule>();

    public virtual ImportBatch? ImportBatch { get; set; }

    public virtual AppUser? PublishedByUser { get; set; }

    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();
}

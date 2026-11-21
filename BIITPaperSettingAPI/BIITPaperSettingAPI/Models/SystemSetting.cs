using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class SystemSetting
{
    public int SettingsId { get; set; }

    public int? ActiveExamCycleId { get; set; }

    public int? ActiveDatesheetId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ExamCycle? ActiveExamCycle { get; set; }

    public virtual Datesheet? Datesheet { get; set; }

    public virtual AppUser? UpdatedByUser { get; set; }
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class ImportError
{
    public int ImportErrorId { get; set; }

    public int ImportBatchId { get; set; }

    public string SheetName { get; set; } = null!;

    public int RowNumber { get; set; }

    public string? ColumnName { get; set; }

    public string ErrorMessage { get; set; } = null!;

    public virtual ImportBatch ImportBatch { get; set; } = null!;
}

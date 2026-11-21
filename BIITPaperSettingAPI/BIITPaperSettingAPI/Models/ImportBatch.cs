using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class ImportBatch
{
    public int ImportBatchId { get; set; }

    public int FileAssetId { get; set; }

    public int ImportedByUserId { get; set; }

    public string ImportStatus { get; set; } = null!;

    public int RowsAdded { get; set; }

    public int RowsUpdated { get; set; }

    public int RowsFailed { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual ICollection<Datesheet> Datesheets { get; set; } = new List<Datesheet>();

    public virtual FileAsset FileAsset { get; set; } = null!;

    public virtual ICollection<ImportError> ImportErrors { get; set; } = new List<ImportError>();

    public virtual AppUser ImportedByUser { get; set; } = null!;
}

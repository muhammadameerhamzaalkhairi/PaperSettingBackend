using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class FileAsset
{
    public int FileAssetId { get; set; }

    public int UploadedByUserId { get; set; }

    public string StorageKey { get; set; } = null!;

    public string OriginalName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public string Checksum { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();

    public virtual ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();

    public virtual ICollection<PrintExport> PrintExports { get; set; } = new List<PrintExport>();

    public virtual ICollection<QuestionAttachment> QuestionAttachments { get; set; } = new List<QuestionAttachment>();

    public virtual AppUser UploadedByUser { get; set; } = null!;
}

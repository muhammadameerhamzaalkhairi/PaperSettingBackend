using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PrintExport
{
    public int PrintExportId { get; set; }

    public int ApprovalReviewId { get; set; }

    public int RequestedByUserId { get; set; }

    public int FileAssetId { get; set; }

    public DateTime GeneratedAt { get; set; }

    public virtual Review ApprovalReview { get; set; } = null!;

    public virtual FileAsset FileAsset { get; set; } = null!;

    public virtual AppUser RequestedByUser { get; set; } = null!;
}

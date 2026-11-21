using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PhysicalReceipt
{
    public int ReceiptId { get; set; }

    public int ApprovalReviewId { get; set; }

    public int ReceivedByUserId { get; set; }

    public int DeliveredByUserId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public string SealStatus { get; set; } = null!;

    public string? Remarks { get; set; }

    public virtual Review ApprovalReview { get; set; } = null!;

    public virtual AppUser DeliveredByUser { get; set; } = null!;

    public virtual AppUser ReceivedByUser { get; set; } = null!;
}

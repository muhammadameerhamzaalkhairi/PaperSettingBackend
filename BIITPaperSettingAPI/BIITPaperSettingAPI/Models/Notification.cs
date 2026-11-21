using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int RecipientUserId { get; set; }

    public int? PaperId { get; set; }

    public int? DelegationId { get; set; }

    public string EventType { get; set; } = null!;

    public string MessageText { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public virtual DirectorDelegation? Delegation { get; set; }

    public virtual Paper? Paper { get; set; }

    public virtual AppUser RecipientUser { get; set; } = null!;
}

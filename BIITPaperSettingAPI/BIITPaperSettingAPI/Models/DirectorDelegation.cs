using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class DirectorDelegation
{
    public int DelegationId { get; set; }

    public int GrantorUserRoleId { get; set; }

    public int DelegateUserId { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public int? RevokedByUserId { get; set; }

    public string? Reason { get; set; }

    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    public virtual AppUser DelegateUser { get; set; } = null!;

    public virtual UserRole GrantorUserRole { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual AppUser? RevokedByUser { get; set; }
}

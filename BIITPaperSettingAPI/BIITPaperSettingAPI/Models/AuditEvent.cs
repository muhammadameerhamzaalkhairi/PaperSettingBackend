using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class AuditEvent
{
    public int AuditEventId { get; set; }

    public int? ActorUserId { get; set; }

    public int? DelegationId { get; set; }

    public string EntityType { get; set; } = null!;

    public string EntityKey { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? BeforeData { get; set; }

    public string? AfterData { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual AppUser? ActorUser { get; set; }

    public virtual DirectorDelegation? Delegation { get; set; }
}

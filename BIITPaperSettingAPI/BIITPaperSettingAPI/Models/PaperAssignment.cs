using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PaperAssignment
{
    public int AssignmentId { get; set; }

    public int PaperId { get; set; }

    public int SetterUserId { get; set; }

    public int AssignedByUserRoleId { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime DueAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public virtual UserRole AssignedByUserRole { get; set; } = null!;

    public virtual Paper Paper { get; set; } = null!;

    public virtual AppUser SetterUser { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class UserRole
{
    public int UserRoleId { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    public int? DepartmentId { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<DirectorDelegation> DirectorDelegations { get; set; } = new List<DirectorDelegation>();

    public virtual ICollection<PaperAssignment> PaperAssignments { get; set; } = new List<PaperAssignment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Role Role { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class VwActiveUserRole
{
    public int UserRoleId { get; set; }

    public int UserId { get; set; }

    public string RoleCode { get; set; } = null!;

    public int? DepartmentId { get; set; }
}

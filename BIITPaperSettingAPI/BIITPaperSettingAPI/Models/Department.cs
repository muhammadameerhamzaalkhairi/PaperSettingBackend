using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Department
{
    public int DepartmentId { get; set; }

    public string DepartmentCode { get; set; } = null!;

    public string DepartmentName { get; set; } = null!;

    public virtual ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();

    public virtual ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<Program> Programs { get; set; } = new List<Program>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public int OwnerDepartmentId { get; set; }

    public string CourseCode { get; set; } = null!;

    public string CourseTitle { get; set; } = null!;

    public virtual ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();

    public virtual ICollection<CourseOutline> CourseOutlines { get; set; } = new List<CourseOutline>();

    public virtual Department OwnerDepartment { get; set; } = null!;

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}

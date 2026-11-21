using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Wishlist
{
    public int WishlistId { get; set; }

    public int OwnerUserId { get; set; }

    public int? CourseId { get; set; }

    public int? PaperId { get; set; }

    public string Title { get; set; } = null!;

    public string? NoteText { get; set; }

    public bool IsDone { get; set; }

    public DateTime? DueAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public virtual Course? Course { get; set; }

    public virtual AppUser? DeletedByUser { get; set; }

    public virtual AppUser OwnerUser { get; set; } = null!;

    public virtual Paper? Paper { get; set; }
}

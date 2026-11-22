using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class ProfileUpdate
{
    [Required,StringLength(200)] public string FullName { get; set; } = "";
    [StringLength(30)] public string? Phone { get; set; }
    [Range(1,int.MaxValue)] public int? PhotoFileId { get; set; }
}

public class WishlistUpdate
{
    [Required,StringLength(500)] public string Title { get; set; } = "";
    public string? Note { get; set; }
    public DateTimeOffset? DueAt { get; set; }
}

public class WishlistCreate : WishlistUpdate
{
    [Range(1,int.MaxValue)] public int? PaperId { get; set; }
}

public class PaperCommentRequest
{
    [Required] public string Text { get; set; } = "";
    [Range(1,int.MaxValue)] public int? PaperItemId { get; set; }
    [Range(1,int.MaxValue)] public int? ParentCommentId { get; set; }
}

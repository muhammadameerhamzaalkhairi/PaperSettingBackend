using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class Paper
{
    public int PaperId { get; set; }

    public int CourseExamId { get; set; }

    public string WorkflowStatus { get; set; } = null!;

    public DateTime? ArchivedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public virtual CourseExam CourseExam { get; set; } = null!;

    public virtual AppUser? DeletedByUser { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual PaperAssignment? PaperAssignment { get; set; }

    public virtual ICollection<PaperParticipant> PaperParticipants { get; set; } = new List<PaperParticipant>();

    public virtual ICollection<PaperVersion> PaperVersions { get; set; } = new List<PaperVersion>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}

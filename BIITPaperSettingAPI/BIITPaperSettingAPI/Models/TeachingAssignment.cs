using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class TeachingAssignment
{
    public int TeachingAssignmentId { get; set; }

    public int OfferingSectionId { get; set; }

    public int TeacherUserId { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public virtual OfferingSection OfferingSection { get; set; } = null!;

    public virtual AppUser TeacherUser { get; set; } = null!;

    public virtual ICollection<TopicCoverageSubmission> TopicCoverageSubmissions { get; set; } = new List<TopicCoverageSubmission>();

    public virtual ICollection<TopicCoverage> TopicCoverages { get; set; } = new List<TopicCoverage>();
}

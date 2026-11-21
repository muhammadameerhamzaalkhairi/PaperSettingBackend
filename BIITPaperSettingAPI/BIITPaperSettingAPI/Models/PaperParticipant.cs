using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class PaperParticipant
{
    public int PaperId { get; set; }

    public int TeacherUserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public virtual Paper Paper { get; set; } = null!;

    public virtual AppUser TeacherUser { get; set; } = null!;
}

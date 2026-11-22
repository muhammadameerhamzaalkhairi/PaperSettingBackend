using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class SectionCoverageRequest
{
    // [] ka matlab teacher ne koi topic select nahi kiya. Missing list invalid hai.
    [Required]
    public List<CoverageTopic?>? Topics { get; set; }
    public bool Submit { get; set; }
}

public class CoverageTopic
{
    [Range(1, int.MaxValue)]
    public int TopicId { get; set; }

    [Required, RegularExpression("^(COVERED|IN_PROGRESS|NOT_STARTED)$")]
    public string Status { get; set; } = "";
}

public class SetterAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [Required]
    public DateTimeOffset? DueAt { get; set; }
}

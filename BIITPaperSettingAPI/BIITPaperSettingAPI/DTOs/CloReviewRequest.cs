using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class CloReviewRequest
{
    [Required, RegularExpression("^(ALIGNED|NOT_ALIGNED|NEEDS_REVIEW)$")]
    public string Status { get; set; } = "";

    [Required]
    public string Explanation { get; set; } = "";
}

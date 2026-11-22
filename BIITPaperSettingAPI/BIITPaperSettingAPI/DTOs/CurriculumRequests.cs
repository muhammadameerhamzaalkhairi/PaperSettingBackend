using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class CreateOutlineRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue)]
    public int VersionNumber { get; set; }
}

public class AddOutlineTopicRequest
{
    [Range(1, int.MaxValue)]
    public int OutlineId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int? ParentTopicId { get; set; }

    [Range(1, int.MaxValue)]
    public int? Week { get; set; }

    [Range(1, int.MaxValue)]
    public int SortOrder { get; set; } = 1;
}

public class AddOutlineCLORequest
{
    [Range(1, int.MaxValue)]
    public int OutlineId { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = "";

    [Required]
    public string Statement { get; set; } = "";
}

public class UpdateOutlineTopicRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int? Week { get; set; }

    [Range(1, int.MaxValue)]
    public int SortOrder { get; set; } = 1;
}

public class UpdateOutlineCLORequest
{
    [Required, MaxLength(40)]
    public string Code { get; set; } = "";

    [Required]
    public string Statement { get; set; } = "";
}

public class ConfigureCourseExamRequest
{
    [Range(1, int.MaxValue)]
    public int OfferingId { get; set; }

    [Range(1, int.MaxValue)]
    public int ExamCycleId { get; set; }

    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal Marks { get; set; }

    [Range(1, int.MaxValue)]
    public int Duration { get; set; }
}

public class UpdateCourseExamSettingsRequest
{
    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal Marks { get; set; }

    [Range(1, int.MaxValue)]
    public int Duration { get; set; }
}

public class SetExamCLOsRequest
{
    [Required, MinLength(1)]
    public List<int> CloIds { get; set; } = [];
}

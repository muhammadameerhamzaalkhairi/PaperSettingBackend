using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class AddDepartmentRequest
{
    [Required, MaxLength(40)]
    public string Code { get; set; } = "";

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
}

public class AddProgramRequest
{
    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = "";

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
}

public class AddSessionRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Required]
    public DateOnly? StartsOn { get; set; }

    [Required]
    public DateOnly? EndsOn { get; set; }
}

public class AddSectionRequest
{
    [Range(1, int.MaxValue)]
    public int ProgramId { get; set; }

    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Range(1, 12)]
    public int SemesterNumber { get; set; }
}

public class AddCourseRequest
{
    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = "";

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";
}

public class AddExamCycleRequest
{
    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }

    [Required, MaxLength(40)]
    public string ExamType { get; set; } = "";
}

public class AddCourseOfferingRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue)]
    public int OutlineId { get; set; }

    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }
}

public class AddOfferingSectionRequest
{
    [Range(1, int.MaxValue)]
    public int OfferingId { get; set; }

    [Range(1, int.MaxValue)]
    public int SectionId { get; set; }
}

public class AssignTeachingRequest
{
    [Range(1, int.MaxValue)]
    public int OfferingSectionId { get; set; }

    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }
}

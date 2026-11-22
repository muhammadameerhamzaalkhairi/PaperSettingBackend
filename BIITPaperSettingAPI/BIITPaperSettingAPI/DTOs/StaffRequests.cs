using System.ComponentModel.DataAnnotations;

namespace BIITPaperSettingAPI.DTOs;

public class CreateUserRequest
{
    [Required, MaxLength(200)]
    public string StaffNumber { get; set; } = "";

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MinLength(12), MaxLength(1024)]
    public string Password { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [MaxLength(100)]
    public string? Designation { get; set; }
}

public class UpdateStaffDetailsRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [MaxLength(100)]
    public string? Designation { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }
}

public class SetStaffActiveRequest
{
    [Required]
    public bool? IsActive { get; set; }
}

public class GrantUserRoleRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required, RegularExpression("^(TEACHER|HOD|DIRECTOR|DATA_CELL)$")]
    public string RoleCode { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }
}

public class GrantDirectorDelegationRequest
{
    [Range(1, int.MaxValue)]
    public int DelegateId { get; set; }

    [Required]
    public DateTimeOffset? StartsAt { get; set; }

    [Required]
    public DateTimeOffset? EndsAt { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.DTOs;

public record SchoolDto(
    Guid Id,
    string Name,
    string ShortName,
    string SchoolCode,
    SchoolType Type,
    string Street,
    string PostalCode,
    string City,
    string? State,
    string? Phone,
    string? Email,
    string? Website,
    SubscriptionTier SubscriptionTier,
    bool IsActive,
    int TeacherCount,
    DateTime CreatedAt
);

public record CreateSchoolDto(
    string Name,
    string ShortName,
    string SchoolCode,
    SchoolType Type,
    string Street,
    string PostalCode,
    string City,
    string? State,
    string? Phone,
    string? Email,
    string? Website
);

public record UpdateSchoolDto(
    string? Name,
    string? ShortName,
    string? Phone,
    string? Email,
    string? Website,
    string? SchoolFoxSchoolId,
    string? WebUntisSchoolName,
    string? WebUntisServer,
    int? DefaultApprovalExpiryHours
);

namespace Lehrerleicht.Approval.Core.DTOs;

public record TeacherDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? Title,
    string? ProfileImageUrl,
    Guid SchoolId,
    string? SchoolName,
    string? Subjects,
    string? Classes,
    bool IsActive,
    string PreferredLanguage,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record UpdateTeacherDto(
    string? FirstName,
    string? LastName,
    string? Title,
    string? ProfileImageUrl,
    string? Subjects,
    string? Classes,
    string? PreferredLanguage,
    string? Timezone
);

public record RegisterTeacherDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Title,
    Guid SchoolId
);

public record LoginDto(
    string Email,
    string Password
);

public record TokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

public record RefreshTokenDto(
    string RefreshToken
);

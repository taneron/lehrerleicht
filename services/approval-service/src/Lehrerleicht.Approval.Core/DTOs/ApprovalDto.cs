using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.DTOs;

public record ApprovalDto(
    Guid Id,
    Guid CorrelationId,
    string TeacherId,
    ApprovalStatus Status,
    PendingActionDto Action,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ProcessedAt,
    bool IsRead,
    DateTime? ReadAt
);

public record ApprovalDetailDto(
    Guid Id,
    Guid CorrelationId,
    string TeacherId,
    ApprovalStatus Status,
    PendingActionDto Action,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ProcessedAt,
    string? ProcessedByDeviceType,
    string? RejectionReason,
    bool IsRead,
    DateTime? ReadAt,
    List<ActionHistoryDto> History
);

public record PendingActionDto(
    Guid Id,
    ActionType Type,
    ActionSource Source,
    Priority Priority,
    string Title,
    string Description,
    string? StudentName,
    string? ClassName,
    string? ParentName,
    string TargetSystem,
    string? OriginalMessagePreview,
    DateTime? OriginalMessageTimestamp,
    double ConfidenceScore,
    string? AiReasoning,
    List<ActionOptionDto> Options
);

public record ApprovalResultDto(
    Guid Id,
    ApprovalStatus Status,
    DateTime ProcessedAt
);

public record ApprovalStatsDto(
    int TotalPending,
    int TotalApproved,
    int TotalRejected,
    int TotalExpired,
    int HighPriorityPending,
    double AverageResponseTimeMinutes
);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

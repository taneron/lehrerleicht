using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.DTOs;

public record ActionHistoryDto(
    Guid Id,
    Guid ApprovalId,
    string? TeacherId,
    ActionHistoryType ActionType,
    string Description,
    string? PreviousState,
    string? NewState,
    DateTime Timestamp
);

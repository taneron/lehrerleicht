namespace Lehrerleicht.Approval.Core.DTOs;

public record ApprovalDecisionDto(
    string? DeviceId,
    string? DeviceType,
    List<OptionResponseDto>? SelectedOptions
);

public record RejectionDto(
    string Reason,
    string? DeviceId,
    string? DeviceType
);

public record OptionResponseDto(
    Guid OptionId,
    string SelectedValueJson
);

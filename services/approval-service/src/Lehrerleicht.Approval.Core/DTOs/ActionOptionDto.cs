using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.DTOs;

public record ActionOptionDto(
    Guid Id,
    ActionOptionType Type,
    string Label,
    string? HelpText,
    bool IsRequired,
    int SortOrder,
    string? ChoicesJson,
    string? SelectedValueJson
);

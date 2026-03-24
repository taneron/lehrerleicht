using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.Entities;

public class ActionOption
{
    public Guid Id { get; set; }

    public Guid PendingActionId { get; set; }
    public PendingAction PendingAction { get; set; } = null!;

    public ActionOptionType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }

    // JSON array of available choices for SingleSelect/MultiSelect
    public string? ChoicesJson { get; set; }

    // Teacher's response — JSON string for FreeText, JSON array for MultiSelect, etc.
    public string? SelectedValueJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

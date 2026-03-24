using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.Entities;

public class PendingAction
{
    public Guid Id { get; set; }

    public Guid ApprovalId { get; set; }
    public ApprovalEntity Approval { get; set; } = null!;

    public ActionType Type { get; set; }
    public ActionSource Source { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }

    public string? StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? ClassName { get; set; }
    public string? ParentName { get; set; }

    public string PayloadJson { get; set; } = "{}";
    public string TargetSystem { get; set; } = string.Empty;

    public string? OriginalMessageId { get; set; }
    public string? OriginalMessagePreview { get; set; }
    public DateTime? OriginalMessageTimestamp { get; set; }

    public double ConfidenceScore { get; set; }
    public string? AiReasoning { get; set; }

    public ICollection<ActionOption> Options { get; set; } = new List<ActionOption>();
}

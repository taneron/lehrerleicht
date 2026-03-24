namespace Lehrerleicht.Approval.Infrastructure.Messaging.Messages;

public class PendingApprovalMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string SchoolId { get; set; } = string.Empty;
    public PendingActionMessage Action { get; set; } = null!;
    public string ExpiresAt { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class PendingActionMessage
{
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? ClassName { get; set; }
    public string? ParentName { get; set; }
    public string Payload { get; set; } = "{}";
    public string TargetSystem { get; set; } = string.Empty;
    public string? OriginalMessageId { get; set; }
    public string? OriginalMessagePreview { get; set; }
    public string? OriginalMessageTimestamp { get; set; }
    public double ConfidenceScore { get; set; }
    public string? AiReasoning { get; set; }
    public List<ActionOptionMessage>? Options { get; set; }
}

public class ActionOptionMessage
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
    public List<string>? Choices { get; set; }
}

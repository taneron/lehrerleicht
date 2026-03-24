using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.Entities;

public class ApprovalEntity
{
    public Guid Id { get; set; }

    public Guid CorrelationId { get; set; }

    public string TeacherId { get; set; } = string.Empty;
    public Teacher Teacher { get; set; } = null!;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public PendingAction Action { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public string? ProcessedByDeviceId { get; set; }
    public string? ProcessedByDeviceType { get; set; }
    public string? RejectionReason { get; set; }

    public bool PushNotificationSent { get; set; }
    public DateTime? PushNotificationSentAt { get; set; }
    public bool EmailNotificationSent { get; set; }
    public DateTime? EmailNotificationSentAt { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public ICollection<ActionHistory> History { get; set; } = new List<ActionHistory>();
}

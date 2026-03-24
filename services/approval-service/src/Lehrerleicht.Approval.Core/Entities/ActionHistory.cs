using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.Entities;

public class ActionHistory
{
    public Guid Id { get; set; }

    public Guid ApprovalId { get; set; }
    public ApprovalEntity Approval { get; set; } = null!;

    public string? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public ActionHistoryType ActionType { get; set; }
    public string Description { get; set; } = string.Empty;

    public string? PreviousState { get; set; }
    public string? NewState { get; set; }

    public string? AdditionalDataJson { get; set; }

    public string? DeviceId { get; set; }
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.Entities;

public class NotificationPreference
{
    public Guid Id { get; set; }

    public string TeacherId { get; set; } = string.Empty;
    public Teacher Teacher { get; set; } = null!;

    public bool PushEnabled { get; set; } = true;
    public bool PushForHighPriority { get; set; } = true;
    public bool PushForNormalPriority { get; set; } = true;
    public bool PushForLowPriority { get; set; }

    public bool EmailEnabled { get; set; } = true;
    public bool EmailDigestEnabled { get; set; }
    public DigestFrequency EmailDigestFrequency { get; set; } = DigestFrequency.Daily;

    public bool QuietHoursEnabled { get; set; } = true;
    public TimeOnly QuietHoursStart { get; set; } = new(20, 0);
    public TimeOnly QuietHoursEnd { get; set; } = new(7, 0);
    public bool QuietHoursWeekendAllDay { get; set; } = true;

    public string? FcmToken { get; set; }
    public string? ApnsToken { get; set; }
    public string? WebPushSubscription { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

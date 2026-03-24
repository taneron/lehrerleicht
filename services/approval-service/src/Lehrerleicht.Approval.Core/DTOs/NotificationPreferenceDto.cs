using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.DTOs;

public record NotificationPreferenceDto(
    bool PushEnabled,
    bool PushForHighPriority,
    bool PushForNormalPriority,
    bool PushForLowPriority,
    bool EmailEnabled,
    bool EmailDigestEnabled,
    DigestFrequency EmailDigestFrequency,
    bool QuietHoursEnabled,
    TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd,
    bool QuietHoursWeekendAllDay
);

public record UpdateNotificationPreferenceDto(
    bool? PushEnabled,
    bool? PushForHighPriority,
    bool? PushForNormalPriority,
    bool? PushForLowPriority,
    bool? EmailEnabled,
    bool? EmailDigestEnabled,
    DigestFrequency? EmailDigestFrequency,
    bool? QuietHoursEnabled,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    bool? QuietHoursWeekendAllDay
);

public record RegisterDeviceDto(
    string? FcmToken,
    string? ApnsToken,
    string? WebPushSubscription
);

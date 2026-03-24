using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Interfaces;

namespace Lehrerleicht.Approval.Core.Services;

public class NotificationService
{
    private readonly INotificationPreferenceRepository _prefRepo;

    public NotificationService(INotificationPreferenceRepository prefRepo)
    {
        _prefRepo = prefRepo;
    }

    public async Task<NotificationPreferenceDto?> GetPreferencesAsync(string teacherId)
    {
        var pref = await _prefRepo.GetByTeacherIdAsync(teacherId);
        if (pref is null) return null;

        return new NotificationPreferenceDto(
            pref.PushEnabled,
            pref.PushForHighPriority,
            pref.PushForNormalPriority,
            pref.PushForLowPriority,
            pref.EmailEnabled,
            pref.EmailDigestEnabled,
            pref.EmailDigestFrequency,
            pref.QuietHoursEnabled,
            pref.QuietHoursStart,
            pref.QuietHoursEnd,
            pref.QuietHoursWeekendAllDay
        );
    }

    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        string teacherId, UpdateNotificationPreferenceDto dto)
    {
        var pref = await _prefRepo.GetByTeacherIdAsync(teacherId)
            ?? new NotificationPreference { TeacherId = teacherId };

        if (dto.PushEnabled.HasValue) pref.PushEnabled = dto.PushEnabled.Value;
        if (dto.PushForHighPriority.HasValue) pref.PushForHighPriority = dto.PushForHighPriority.Value;
        if (dto.PushForNormalPriority.HasValue) pref.PushForNormalPriority = dto.PushForNormalPriority.Value;
        if (dto.PushForLowPriority.HasValue) pref.PushForLowPriority = dto.PushForLowPriority.Value;
        if (dto.EmailEnabled.HasValue) pref.EmailEnabled = dto.EmailEnabled.Value;
        if (dto.EmailDigestEnabled.HasValue) pref.EmailDigestEnabled = dto.EmailDigestEnabled.Value;
        if (dto.EmailDigestFrequency.HasValue) pref.EmailDigestFrequency = dto.EmailDigestFrequency.Value;
        if (dto.QuietHoursEnabled.HasValue) pref.QuietHoursEnabled = dto.QuietHoursEnabled.Value;
        if (dto.QuietHoursStart.HasValue) pref.QuietHoursStart = dto.QuietHoursStart.Value;
        if (dto.QuietHoursEnd.HasValue) pref.QuietHoursEnd = dto.QuietHoursEnd.Value;
        if (dto.QuietHoursWeekendAllDay.HasValue) pref.QuietHoursWeekendAllDay = dto.QuietHoursWeekendAllDay.Value;

        pref.UpdatedAt = DateTime.UtcNow;
        var updated = await _prefRepo.CreateOrUpdateAsync(pref);

        return new NotificationPreferenceDto(
            updated.PushEnabled,
            updated.PushForHighPriority,
            updated.PushForNormalPriority,
            updated.PushForLowPriority,
            updated.EmailEnabled,
            updated.EmailDigestEnabled,
            updated.EmailDigestFrequency,
            updated.QuietHoursEnabled,
            updated.QuietHoursStart,
            updated.QuietHoursEnd,
            updated.QuietHoursWeekendAllDay
        );
    }

    public async Task RegisterDeviceAsync(string teacherId, RegisterDeviceDto dto)
    {
        var pref = await _prefRepo.GetByTeacherIdAsync(teacherId)
            ?? new NotificationPreference { TeacherId = teacherId };

        if (dto.FcmToken is not null) pref.FcmToken = dto.FcmToken;
        if (dto.ApnsToken is not null) pref.ApnsToken = dto.ApnsToken;
        if (dto.WebPushSubscription is not null) pref.WebPushSubscription = dto.WebPushSubscription;

        pref.UpdatedAt = DateTime.UtcNow;
        await _prefRepo.CreateOrUpdateAsync(pref);
    }

    public async Task UnregisterDeviceAsync(string teacherId)
    {
        var pref = await _prefRepo.GetByTeacherIdAsync(teacherId);
        if (pref is null) return;

        pref.FcmToken = null;
        pref.ApnsToken = null;
        pref.WebPushSubscription = null;
        pref.UpdatedAt = DateTime.UtcNow;
        await _prefRepo.CreateOrUpdateAsync(pref);
    }
}

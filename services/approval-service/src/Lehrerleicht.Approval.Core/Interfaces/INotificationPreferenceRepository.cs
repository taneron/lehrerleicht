using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Core.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByTeacherIdAsync(string teacherId);
    Task<NotificationPreference> CreateOrUpdateAsync(NotificationPreference preference);
}

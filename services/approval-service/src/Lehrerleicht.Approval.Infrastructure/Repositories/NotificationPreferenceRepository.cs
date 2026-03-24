using Microsoft.EntityFrameworkCore;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Interfaces;
using Lehrerleicht.Approval.Infrastructure.Data;

namespace Lehrerleicht.Approval.Infrastructure.Repositories;

public class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly ApprovalDbContext _db;

    public NotificationPreferenceRepository(ApprovalDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationPreference?> GetByTeacherIdAsync(string teacherId)
    {
        return await _db.NotificationPreferences
            .FirstOrDefaultAsync(n => n.TeacherId == teacherId);
    }

    public async Task<NotificationPreference> CreateOrUpdateAsync(NotificationPreference preference)
    {
        var existing = await _db.NotificationPreferences
            .FirstOrDefaultAsync(n => n.TeacherId == preference.TeacherId);

        if (existing is null)
        {
            _db.NotificationPreferences.Add(preference);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(preference);
            preference = existing;
        }

        await _db.SaveChangesAsync();
        return preference;
    }
}

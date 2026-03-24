using Microsoft.AspNetCore.Identity;

namespace Lehrerleicht.Approval.Core.Entities;

public class Teacher : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ProfileImageUrl { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public string? Subjects { get; set; }
    public string? Classes { get; set; }

    public string? SchoolFoxTeacherId { get; set; }
    public string? WebUntisTeacherId { get; set; }

    public bool IsActive { get; set; } = true;
    public string PreferredLanguage { get; set; } = "de";
    public string Timezone { get; set; } = "Europe/Vienna";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApprovalEntity> Approvals { get; set; } = new List<ApprovalEntity>();
    public ICollection<ActionHistory> ActionHistories { get; set; } = new List<ActionHistory>();
    public NotificationPreference? NotificationPreference { get; set; }
}

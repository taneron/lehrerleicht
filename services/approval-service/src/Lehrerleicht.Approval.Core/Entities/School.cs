using Lehrerleicht.Approval.Core.Enums;

namespace Lehrerleicht.Approval.Core.Entities;

public class School
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string SchoolCode { get; set; } = string.Empty;

    public SchoolType Type { get; set; }

    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Austria";
    public string? State { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string? SchoolFoxSchoolId { get; set; }
    public string? WebUntisSchoolName { get; set; }
    public string? WebUntisServer { get; set; }

    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public DateTime? SubscriptionExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
    public int DefaultApprovalExpiryHours { get; set; } = 24;
    public string Timezone { get; set; } = "Europe/Vienna";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}

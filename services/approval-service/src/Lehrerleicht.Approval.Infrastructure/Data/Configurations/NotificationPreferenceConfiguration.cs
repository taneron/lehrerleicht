using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.FcmToken).HasMaxLength(500);
        builder.Property(n => n.ApnsToken).HasMaxLength(500);
        builder.Property(n => n.WebPushSubscription).HasColumnType("jsonb");

        builder.HasOne(n => n.Teacher)
            .WithOne(t => t.NotificationPreference)
            .HasForeignKey<NotificationPreference>(n => n.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.TeacherId).IsUnique();
    }
}

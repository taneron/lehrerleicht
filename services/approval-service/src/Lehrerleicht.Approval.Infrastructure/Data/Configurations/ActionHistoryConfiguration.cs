using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data.Configurations;

public class ActionHistoryConfiguration : IEntityTypeConfiguration<ActionHistory>
{
    public void Configure(EntityTypeBuilder<ActionHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Description).HasMaxLength(1000).IsRequired();
        builder.Property(h => h.PreviousState).HasMaxLength(50);
        builder.Property(h => h.NewState).HasMaxLength(50);
        builder.Property(h => h.AdditionalDataJson).HasColumnType("jsonb");
        builder.Property(h => h.DeviceId).HasMaxLength(200);
        builder.Property(h => h.DeviceType).HasMaxLength(20);
        builder.Property(h => h.IpAddress).HasMaxLength(50);
        builder.Property(h => h.UserAgent).HasMaxLength(500);

        builder.HasOne(h => h.Approval)
            .WithMany(a => a.History)
            .HasForeignKey(h => h.ApprovalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Teacher)
            .WithMany(t => t.ActionHistories)
            .HasForeignKey(h => h.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => h.ApprovalId);
        builder.HasIndex(h => h.TeacherId);
        builder.HasIndex(h => h.Timestamp);
    }
}

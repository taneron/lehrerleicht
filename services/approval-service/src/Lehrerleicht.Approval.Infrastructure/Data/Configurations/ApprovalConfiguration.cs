using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data.Configurations;

public class ApprovalConfiguration : IEntityTypeConfiguration<ApprovalEntity>
{
    public void Configure(EntityTypeBuilder<ApprovalEntity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ProcessedByDeviceId).HasMaxLength(200);
        builder.Property(a => a.ProcessedByDeviceType).HasMaxLength(20);
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);

        builder.HasOne(a => a.Teacher)
            .WithMany(t => t.Approvals)
            .HasForeignKey(a => a.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Action)
            .WithOne(pa => pa.Approval)
            .HasForeignKey<PendingAction>(pa => pa.ApprovalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.TeacherId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.ExpiresAt);
        builder.HasIndex(a => a.CorrelationId);
    }
}

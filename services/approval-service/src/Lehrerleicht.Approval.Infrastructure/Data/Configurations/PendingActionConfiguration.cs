using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data.Configurations;

public class PendingActionConfiguration : IEntityTypeConfiguration<PendingAction>
{
    public void Configure(EntityTypeBuilder<PendingAction> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.IconUrl).HasMaxLength(500);
        builder.Property(p => p.StudentId).HasMaxLength(100);
        builder.Property(p => p.StudentName).HasMaxLength(200);
        builder.Property(p => p.ClassName).HasMaxLength(20);
        builder.Property(p => p.ParentName).HasMaxLength(200);
        builder.Property(p => p.PayloadJson).HasColumnType("jsonb");
        builder.Property(p => p.TargetSystem).HasMaxLength(50).IsRequired();
        builder.Property(p => p.OriginalMessageId).HasMaxLength(200);
        builder.Property(p => p.OriginalMessagePreview).HasMaxLength(500);
        builder.Property(p => p.AiReasoning).HasMaxLength(2000);

        builder.HasIndex(p => p.StudentId);
    }
}

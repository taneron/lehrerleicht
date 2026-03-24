using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data.Configurations;

public class ActionOptionConfiguration : IEntityTypeConfiguration<ActionOption>
{
    public void Configure(EntityTypeBuilder<ActionOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Label).HasMaxLength(500).IsRequired();
        builder.Property(o => o.HelpText).HasMaxLength(1000);
        builder.Property(o => o.ChoicesJson).HasColumnType("jsonb");
        builder.Property(o => o.SelectedValueJson).HasColumnType("jsonb");

        builder.HasOne(o => o.PendingAction)
            .WithMany(p => p.Options)
            .HasForeignKey(o => o.PendingActionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.PendingActionId);
    }
}

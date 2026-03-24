using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.Property(t => t.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.LastName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(50);
        builder.Property(t => t.ProfileImageUrl).HasMaxLength(500);
        builder.Property(t => t.Subjects).HasMaxLength(500);
        builder.Property(t => t.Classes).HasMaxLength(200);
        builder.Property(t => t.SchoolFoxTeacherId).HasMaxLength(100);
        builder.Property(t => t.WebUntisTeacherId).HasMaxLength(100);
        builder.Property(t => t.PreferredLanguage).HasMaxLength(10).HasDefaultValue("de");
        builder.Property(t => t.Timezone).HasMaxLength(50).HasDefaultValue("Europe/Vienna");

        builder.HasOne(t => t.School)
            .WithMany(s => s.Teachers)
            .HasForeignKey(t => t.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.SchoolId);
        builder.HasIndex(t => t.IsActive);
    }
}

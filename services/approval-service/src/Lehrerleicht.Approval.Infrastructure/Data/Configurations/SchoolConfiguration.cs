using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data.Configurations;

public class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ShortName).HasMaxLength(50).IsRequired();
        builder.Property(s => s.SchoolCode).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Street).HasMaxLength(200);
        builder.Property(s => s.PostalCode).HasMaxLength(10);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.Country).HasMaxLength(50).HasDefaultValue("Austria");
        builder.Property(s => s.State).HasMaxLength(50);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Website).HasMaxLength(300);
        builder.Property(s => s.SchoolFoxSchoolId).HasMaxLength(100);
        builder.Property(s => s.WebUntisSchoolName).HasMaxLength(100);
        builder.Property(s => s.WebUntisServer).HasMaxLength(200);
        builder.Property(s => s.Timezone).HasMaxLength(50).HasDefaultValue("Europe/Vienna");

        builder.HasIndex(s => s.SchoolCode).IsUnique();
    }
}

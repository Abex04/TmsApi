using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        // Primary key
        builder.HasKey(s => s.Id);

        // RegistrationNumber is required and max 20 characters
        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(20);

        // Name is required and max 100 characters
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        // GPA has 2 decimal places
        builder.Property(s => s.GPA)
            .HasPrecision(4, 2);

        // IsActive defaults to true
        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);
    }
}

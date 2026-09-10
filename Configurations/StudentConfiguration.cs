using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.GPA)
            .HasPrecision(4, 2);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);

        // Links a Student record to the TmsUser (Identity) account used to
        // log in. Nullable and unique - each login account maps to at most
        // one Student record, and pre-existing seed Students have none.
        builder.Property(s => s.TmsUserId)
            .HasMaxLength(450); // matches ASP.NET Identity's default key length

        builder.HasIndex(s => s.TmsUserId)
            .IsUnique();

        // Shadow property — exists in DB but not in C# class
        // Tracks when record was last updated without cluttering the DTO
        builder.Property<DateTime>("LastUpdated");

        // Soft delete filter — IsDeleted students hidden from all normal queries
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

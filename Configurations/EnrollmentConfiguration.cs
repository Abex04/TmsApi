using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        // Primary key
        builder.HasKey(e => e.Id);

        // Grade is optional (nullable decimal)
        builder.Property(e => e.Grade)
            .HasPrecision(4, 2);

        // Relationship: one Student has many Enrollments
        // Restrict delete — cannot delete a student who has enrollments
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: one Course has many Enrollments
        // Restrict delete — cannot delete a course that has enrollments
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

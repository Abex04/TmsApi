using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        // Primary key
        builder.HasKey(c => c.Id);

        // Code is required and max 10 characters
        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(10);

        // Unique index on Code — prevents two courses from sharing the same code
        // at the database level. This is what makes the duplicate-code check in
        // Module 6 Exercise 3 a real business rule, not just a courtesy check.
        builder.HasIndex(c => c.Code)
            .IsUnique();

        // Title is required and max 200 characters
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        // MaxCapacity must be provided (renamed from Capacity in Module 6)
        builder.Property(c => c.MaxCapacity)
            .IsRequired();
    }
}

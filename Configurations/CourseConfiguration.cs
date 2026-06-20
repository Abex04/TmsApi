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

        // Title is required and max 200 characters
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Capacity must be positive
        builder.Property(c => c.Capacity)
            .IsRequired();
    }
}

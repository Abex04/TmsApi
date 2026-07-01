using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Services;

// Real implementation of ICourseService, backed by TmsDbContext (PostgreSQL via EF Core).
// Primary constructor: "context" and "logger" are injected by ASP.NET Core's DI container
// and can be used directly anywhere in this class, without a manual constructor body.
public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    public async Task<Course?> GetByIdAsync(int id, CancellationToken ct)
    {
        // AsNoTracking() = read-only fetch. We are not going to modify this course,
        // so we skip EF's change-tracking overhead for better performance.
        return await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Course> CreateAsync(Course course, CancellationToken ct)
    {
        // Add() stages the new course in memory; nothing is sent to the database yet.
        context.Courses.Add(course);

        // SaveChangesAsync() actually executes the INSERT statement against PostgreSQL.
        await context.SaveChangesAsync(ct);

        // Log a breadcrumb so we can see in production logs exactly which course was created and when.
        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);

        // course.Id is now populated by EF Core with the database-generated identity value.
        return course;
    }
}